using FanControl.AiPlugin.Config;
using FanControl.AiPlugin.Logging;
using FanControl.AiPlugin.Models;
using FanControl.AiPlugin.Sensors;
using FanControl.AiPlugin.Services;

namespace FanControl.AiPlugin.Plugin;

/// <summary>
/// FanControl 插件适配器：连接 AI 决策层与 FanControl 插件接口。
/// 管理传感器数据采集、AI 调用及安全校验。
///
/// 设计：优先由 IPlugin2.Update() 驱动（TickOnce），
/// 不再默认启动后台轮询线程。FanControl 主循环会定时调用 Update()，
/// 适配器在其中按 pollingIntervalSeconds 频率节流 AI 请求。
/// </summary>
public sealed class FanControlPluginAdapter : IDisposable
{
    private readonly AiProviderSettings _settings;
    private readonly ISensorProvider _sensor;
    private readonly AiDecisionService _aiService;
    private readonly PluginLogger _logger;
    private readonly DiagnosticsSummary _diagnostics;

    private FanRuntimeSnapshot? _lastSnapshot;
    private AiFanDecision? _lastDecision;
    private DateTime _lastAiCallUtc = DateTime.MinValue;
    private readonly object _lock = new();

    // ── AI 调用优化状态 ──
    private FanRuntimeSnapshot? _lastAiCallSnapshot;  // 上次实际调用 AI 时的快照
    private readonly Queue<FanRuntimeSnapshot> _snapshotHistory = new();

    // ── 传感器清洗状态 ──
    private FanRuntimeSnapshot? _lastGoodSnapshot;  // 上次清洗后的正常快照

    /// <summary>最新的安全校验后决策（线程安全读取）</summary>
    public AiFanDecision? LastDecision
    {
        get { lock (_lock) return _lastDecision; }
    }

    /// <summary>最新快照</summary>
    public FanRuntimeSnapshot? LastSnapshot
    {
        get { lock (_lock) return _lastSnapshot; }
    }

    /// <summary>诊断摘要（只读）</summary>
    public DiagnosticsSummary Diagnostics => _diagnostics;

    /// <summary>决策更新回调（可选）</summary>
    public event Action<AiFanDecision>? OnDecisionUpdated;

    public FanControlPluginAdapter(AiProviderSettings settings, ISensorProvider sensor, PluginLogger? logger = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _sensor = sensor ?? throw new ArgumentNullException(nameof(sensor));
        _logger = logger ?? new PluginLogger();
        _aiService = new AiDecisionService(settings, _logger);

        _diagnostics = new DiagnosticsSummary
        {
            SensorProviderType = sensor.GetType().Name,
            DiagnosticsEnabled = settings.EnableDiagnostics,
            LogLevel = settings.LogLevel,
            LogFilePath = _logger.LogFilePath,
            StartTimeUtc = DateTime.UtcNow
        };

        // 如果传感器提供者支持绑定结果，填充诊断信息
#if USE_LIBRE_HARDWARE_MONITOR
        if (sensor is LibreHardwareMonitorSensorProvider lhm)
        {
            _diagnostics.SensorBindingStatus = lhm.GetBindingStatus();
            _diagnostics.SensorBindingResults = lhm.GetBindingResults();
        }
#endif

        _logger.Info("Adapter", $"适配器已创建，传感器: {sensor.GetType().Name}");
    }

    /// <summary>
    /// 同步单次驱动——由 IPlugin2.Update() 调用。
    /// 每次调用采集传感器数据；按 pollingIntervalSeconds 节流 AI 请求。
    /// 支持 changeThreshold（变化阈值跳过）和 hysteresisPercent（迟滞死区）优化。
    /// </summary>
    public void TickOnce()
    {
        // 采集传感器
        var snapshot = _sensor.Collect(_lastSnapshot);

        // 传感器数据清洗
        if (_settings.EnableSensorSanitize)
        {
            snapshot = SensorSanitizer.Sanitize(snapshot, _lastGoodSnapshot, _logger);
            _lastGoodSnapshot = snapshot;
        }

        lock (_lock) _lastSnapshot = snapshot;
        _diagnostics.LastSnapshot = snapshot;
        PushSnapshotHistory(snapshot);

        // 检查是否该发起 AI 请求
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastAiCallUtc).TotalSeconds;
        if (elapsed < _settings.PollingIntervalSeconds)
        {
            _logger.Debug("Adapter", $"TickOnce: 节流中（已过 {elapsed:F1}s / 需要 {_settings.PollingIntervalSeconds}s），仅刷新传感器");
            return; // 本周期只刷新传感器，不调 AI
        }

        // changeThreshold: 如果温度/负载变化不显著，跳过本次 AI 调用
        if (_settings.ChangeThreshold > 0 && _lastAiCallSnapshot is not null && !IsSignificantChange(snapshot, _lastAiCallSnapshot))
        {
            _logger.Debug("Adapter", $"TickOnce: 变化未超过阈值 {_settings.ChangeThreshold}°C，跳过 AI 调用");
            return;
        }

        _lastAiCallUtc = now;
        _lastAiCallSnapshot = snapshot;
        _logger.Info("Adapter", $"TickOnce: 节流到期，发起 AI 调用（间隔 {elapsed:F1}s）");

        try
        {
            var historyList = GetSnapshotHistoryList();
            AiFanDecision? prevDecision;
            lock (_lock) prevDecision = _lastDecision;
            var (raw, safe) = _aiService.GetDecisionSync(snapshot, historyList, prevDecision);

            // hysteresisPercent: 如果风扇变化在死区内，沿用上次决策
            safe = ApplyHysteresis(safe);

            lock (_lock) _lastDecision = safe;
            _diagnostics.LastDecision = safe;
            _diagnostics.LastAiCallUtc = now;

            if (raw is not null)
                _diagnostics.AiCallSuccessCount++;
            else
                _diagnostics.LocalFallbackCount++;

            if (raw is not null && WasCorrected(raw, safe))
                _diagnostics.SafetyGuardCorrectionCount++;

            OnDecisionUpdated?.Invoke(safe);

            _logger.Info("Adapter", $"AI 决策完成: CPU={safe.CpuFanPercent:F1}% GPU={safe.GpuFanPercent:F1}% Case={safe.CaseFanPercent:F1}% 模式={safe.Mode} 来源={(safe.IsFromAi ? "AI" : "回退")}");
        }
        catch (Exception ex)
        {
            _diagnostics.AiCallFailureCount++;
            _logger.Error("Adapter", "AI 调用异常", ex);
        }
    }

    /// <summary>
    /// 异步单次驱动——供 Demo 或异步场景使用。
    /// </summary>
    public async Task TickOnceAsync()
    {
        var snapshot = _sensor.Collect(_lastSnapshot);

        if (_settings.EnableSensorSanitize)
        {
            snapshot = SensorSanitizer.Sanitize(snapshot, _lastGoodSnapshot, _logger);
            _lastGoodSnapshot = snapshot;
        }

        lock (_lock) _lastSnapshot = snapshot;
        _diagnostics.LastSnapshot = snapshot;
        PushSnapshotHistory(snapshot);

        var now = DateTime.UtcNow;
        var elapsed = (now - _lastAiCallUtc).TotalSeconds;
        if (elapsed < _settings.PollingIntervalSeconds)
        {
            _logger.Debug("Adapter", $"TickOnceAsync: 节流中（已过 {elapsed:F1}s）");
            return;
        }

        // changeThreshold: 如果变化不显著，跳过 AI 调用
        if (_settings.ChangeThreshold > 0 && _lastAiCallSnapshot is not null && !IsSignificantChange(snapshot, _lastAiCallSnapshot))
        {
            _logger.Debug("Adapter", $"TickOnceAsync: 变化未超过阈值 {_settings.ChangeThreshold}°C，跳过 AI 调用");
            return;
        }

        _lastAiCallUtc = now;
        _lastAiCallSnapshot = snapshot;
        _logger.Info("Adapter", "TickOnceAsync: 发起 AI 调用");

        try
        {
            var historyList = GetSnapshotHistoryList();
            AiFanDecision? prevDecision;
            lock (_lock) prevDecision = _lastDecision;
            var (raw, safe) = await _aiService.GetDecisionAsync(snapshot, historyList, prevDecision);

            safe = ApplyHysteresis(safe);

            lock (_lock) _lastDecision = safe;
            _diagnostics.LastDecision = safe;
            _diagnostics.LastAiCallUtc = now;

            if (raw is not null)
                _diagnostics.AiCallSuccessCount++;
            else
                _diagnostics.LocalFallbackCount++;

            if (raw is not null && WasCorrected(raw, safe))
                _diagnostics.SafetyGuardCorrectionCount++;

            OnDecisionUpdated?.Invoke(safe);
        }
        catch (Exception ex)
        {
            _diagnostics.AiCallFailureCount++;
            _logger.Error("Adapter", "AI 调用异常", ex);
        }
    }

    /// <summary>
    /// 强制立即执行一次 AI 决策（忽略节流和 changeThreshold），用于初始化或测试。
    /// </summary>
    public async Task ForceTickAsync()
    {
        _logger.Info("Adapter", "ForceTickAsync: 强制执行 AI 决策（跳过节流）");

        var snapshot = _sensor.Collect(_lastSnapshot);

        if (_settings.EnableSensorSanitize)
        {
            snapshot = SensorSanitizer.Sanitize(snapshot, _lastGoodSnapshot, _logger);
            _lastGoodSnapshot = snapshot;
        }

        lock (_lock) _lastSnapshot = snapshot;
        _diagnostics.LastSnapshot = snapshot;
        PushSnapshotHistory(snapshot);

        _lastAiCallUtc = DateTime.UtcNow;
        _lastAiCallSnapshot = snapshot;

        try
        {
            var historyList = GetSnapshotHistoryList();
            AiFanDecision? prevDecision;
            lock (_lock) prevDecision = _lastDecision;
            var (raw, safe) = await _aiService.GetDecisionAsync(snapshot, historyList, prevDecision);
            lock (_lock) _lastDecision = safe;
            _diagnostics.LastDecision = safe;
            _diagnostics.LastAiCallUtc = _lastAiCallUtc;

            if (raw is not null)
                _diagnostics.AiCallSuccessCount++;
            else
                _diagnostics.LocalFallbackCount++;

            if (raw is not null && WasCorrected(raw, safe))
                _diagnostics.SafetyGuardCorrectionCount++;

            OnDecisionUpdated?.Invoke(safe);
        }
        catch (Exception ex)
        {
            _diagnostics.AiCallFailureCount++;
            _logger.Error("Adapter", "ForceTickAsync: AI 调用异常", ex);
            throw;
        }
    }

    /// <summary>判断安全守卫是否修正了决策值</summary>
    private static bool WasCorrected(AiFanDecision before, AiFanDecision after)
    {
        const double eps = 0.01;
        return Math.Abs(before.CpuFanPercent - after.CpuFanPercent) > eps
            || Math.Abs(before.GpuFanPercent - after.GpuFanPercent) > eps
            || Math.Abs(before.CaseFanPercent - after.CaseFanPercent) > eps
            || before.Mode != after.Mode;
    }

    // ── changeThreshold: 判断温度/负载变化是否显著 ──

    /// <summary>
    /// 检查当前快照与上次 AI 调用时的快照之间是否存在显著变化。
    /// 任一温度或负载变化超过 changeThreshold 即视为显著。
    /// </summary>
    private bool IsSignificantChange(FanRuntimeSnapshot current, FanRuntimeSnapshot baseline)
    {
        var threshold = _settings.ChangeThreshold;
        return Math.Abs(current.CpuTemperature - baseline.CpuTemperature) >= threshold
            || Math.Abs(current.GpuTemperature - baseline.GpuTemperature) >= threshold
            || Math.Abs(current.MotherboardTemperature - baseline.MotherboardTemperature) >= threshold
            || Math.Abs(current.CpuUsagePercent - baseline.CpuUsagePercent) >= threshold * 5
            || Math.Abs(current.GpuUsagePercent - baseline.GpuUsagePercent) >= threshold * 5;
    }

    // ── hysteresisPercent: 迟滞死区，防止风扇频繁微调 ──

    /// <summary>
    /// 如果新决策与上次决策的风扇转速差异在迟滞死区内，沿用上次决策值。
    /// </summary>
    private AiFanDecision ApplyHysteresis(AiFanDecision newDecision)
    {
        var hyst = _settings.HysteresisPercent;
        if (hyst <= 0) return newDecision;

        AiFanDecision? prev;
        lock (_lock) prev = _lastDecision;
        if (prev is null) return newDecision;

        var cpuDelta = Math.Abs(newDecision.CpuFanPercent - prev.CpuFanPercent);
        var gpuDelta = Math.Abs(newDecision.GpuFanPercent - prev.GpuFanPercent);
        var caseDelta = Math.Abs(newDecision.CaseFanPercent - prev.CaseFanPercent);

        if (cpuDelta < hyst) newDecision.CpuFanPercent = prev.CpuFanPercent;
        if (gpuDelta < hyst) newDecision.GpuFanPercent = prev.GpuFanPercent;
        if (caseDelta < hyst) newDecision.CaseFanPercent = prev.CaseFanPercent;

        if (cpuDelta < hyst || gpuDelta < hyst || caseDelta < hyst)
            _logger.Debug("Adapter", $"迟滞死区(+/-{hyst}%): 部分风扇沿用上次值");

        return newDecision;
    }

    // ── 快照历史管理 ──

    /// <summary>将快照加入历史队列，保持队列长度不超过 SnapshotHistorySize</summary>
    private void PushSnapshotHistory(FanRuntimeSnapshot snapshot)
    {
        if (_settings.SnapshotHistorySize <= 0) return;
        _snapshotHistory.Enqueue(snapshot);
        while (_snapshotHistory.Count > _settings.SnapshotHistorySize)
            _snapshotHistory.Dequeue();
    }

    /// <summary>获取快照历史的只读列表副本</summary>
    private List<FanRuntimeSnapshot> GetSnapshotHistoryList()
    {
        return _snapshotHistory.ToList();
    }

    /// <summary>测试 AI 连接</summary>
    public Task<(bool Success, string Message)> TestConnectionAsync()
        => _aiService.TestConnectionAsync();

    public void Dispose()
    {
        _logger.Info("Adapter", "适配器正在释放资源");
        _aiService.Dispose();
        _sensor.Dispose();
    }
}
