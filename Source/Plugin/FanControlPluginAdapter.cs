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

        _logger.Info("Adapter", $"\u9002\u914d\u5668\u5df2\u521b\u5efa\uff0c\u4f20\u611f\u5668: {sensor.GetType().Name}");
    }

    /// <summary>
    /// 同步单次驱动——由 IPlugin2.Update() 调用。
    /// 每次调用采集传感器数据；按 pollingIntervalSeconds 节流 AI 请求。
    /// FanControl 的 Update 频率通常为 1 秒，而 AI 请求频率可能为 5 秒，
    /// 中间的周期只刷新传感器数据，不发起 AI 调用。
    /// </summary>
    public void TickOnce()
    {
        // 采集传感器
        var snapshot = _sensor.Collect(_lastSnapshot);
        lock (_lock) _lastSnapshot = snapshot;
        _diagnostics.LastSnapshot = snapshot;

        // 检查是否该发起 AI 请求
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastAiCallUtc).TotalSeconds;
        if (elapsed < _settings.PollingIntervalSeconds)
        {
            _logger.Debug("Adapter", $"TickOnce: 节流中（已过 {elapsed:F1}s / 需要 {_settings.PollingIntervalSeconds}s），仅刷新传感器");
            return; // 本周期只刷新传感器，不调 AI
        }

        _lastAiCallUtc = now;
        _logger.Info("Adapter", $"TickOnce: 节流到期，发起 AI 调用（间隔 {elapsed:F1}s）");

        try
        {
            var (raw, safe) = _aiService.GetDecisionSync(snapshot);
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
        lock (_lock) _lastSnapshot = snapshot;
        _diagnostics.LastSnapshot = snapshot;

        var now = DateTime.UtcNow;
        var elapsed = (now - _lastAiCallUtc).TotalSeconds;
        if (elapsed < _settings.PollingIntervalSeconds)
        {
            _logger.Debug("Adapter", $"TickOnceAsync: 节流中（已过 {elapsed:F1}s）");
            return;
        }

        _lastAiCallUtc = now;
        _logger.Info("Adapter", "TickOnceAsync: 发起 AI 调用");

        try
        {
            var (raw, safe) = await _aiService.GetDecisionAsync(snapshot);
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
    /// 强制立即执行一次 AI 决策（忽略节流），用于初始化或测试。
    /// </summary>
    public async Task ForceTickAsync()
    {
        _logger.Info("Adapter", "ForceTickAsync: 强制执行 AI 决策（跳过节流）");

        var snapshot = _sensor.Collect(_lastSnapshot);
        lock (_lock) _lastSnapshot = snapshot;
        _diagnostics.LastSnapshot = snapshot;

        _lastAiCallUtc = DateTime.UtcNow;

        try
        {
            var (raw, safe) = await _aiService.GetDecisionAsync(snapshot);
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
