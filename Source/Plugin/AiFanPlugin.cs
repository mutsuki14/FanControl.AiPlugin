using FanControl.Plugins;
using FanControl.AiPlugin.Config;
using FanControl.AiPlugin.Logging;
using FanControl.AiPlugin.Sensors;

namespace FanControl.AiPlugin.Plugin;

/// <summary>
/// AI 风扇控制插件主入口——实现真实 FanControl.Plugins.IPlugin2 接口。
/// FanControl 启动时扫描 Plugins 目录，发现此类后自动加载。
///
/// 生命周期：
///   1. FanControl 实例化此类
///   2. 调用 Initialize()
///   3. 调用 Load(container) → 注册温度传感器和控制传感器
///   4. 每个轮询周期调用 Update() → 驱动 AI 决策
///   5. 退出时调用 Close()
///
/// 传感器提供者切换：
///   默认使用 MockSensorProvider（模拟数据），
///   如需使用 LibreHardwareMonitor 真实传感器，需满足：
///     1. 以 dotnet build -p:USE_LHM=true 编译
///     2. 配置文件 sensorProvider 设为 "lhm"
///     3. 以管理员权限运行
/// </summary>
public sealed class AiFanPlugin : IPlugin2
{
    private FanControlPluginAdapter? _adapter;
    private PluginLogger? _logger;

    // ── 温度传感器（注册到 container.TempSensors） ──
    private AiTempSensor? _cpuTempSensor;
    private AiTempSensor? _gpuTempSensor;
    private AiTempSensor? _mbTempSensor;

    // ── 控制传感器（注册到 container.ControlSensors） ──
    private AiControlSensor? _cpuFanCtrl;
    private AiControlSensor? _gpuFanCtrl;
    private AiControlSensor? _caseFanCtrl;

    /// <summary>插件名称（显示在 FanControl 界面中）</summary>
    public string Name => "AI Fan Control";

    /// <summary>
    /// FanControl 在发现插件后调用，用于初始化。
    /// 此时不创建传感器（传感器在 Load 中注册）。
    /// </summary>
    public void Initialize()
    {
        // 初始化阶段可以做日志、预检查等
    }

    /// <summary>
    /// FanControl 调用此方法让插件注册传感器和控制器。
    /// 参数 _container 的属性：TempSensors、FanSensors、ControlSensors（均为 List）。
    /// </summary>
    public void Load(IPluginSensorsContainer _container)
    {
        // 加载配置
        var settings = SettingsStore.Load();

        // 初始化日志服务
        _logger = new PluginLogger(
            settings.EnableDiagnostics,
            PluginLogger.ParseLevel(settings.LogLevel),
            settings.LogToFile);

        _logger.Info("Plugin", "========== AiFanPlugin.Load() 开始 ==========");
        _logger.Info("Plugin", $"配置: {settings}");

        // ── 创建传感器绑定配置 ──
        var bindingConfig = SensorBindingConfig.FromSettings(settings);
        if (bindingConfig.HasAnyBinding)
            _logger.Info("Plugin", $"\u4f20\u611f\u5668\u7ed1\u5b9a\u914d\u7f6e: CPU={bindingConfig.CpuSensorName}, GPU={bindingConfig.GpuSensorName}, MB={bindingConfig.MotherboardSensorName} (\u6a21\u5f0f={(!bindingConfig.UseExactMatch ? "contains" : "exact")})");
        else
            _logger.Info("Plugin", "\u672a\u914d\u7f6e\u4f20\u611f\u5668\u540d\u79f0\u7ed1\u5b9a\uff0c\u5c06\u4f7f\u7528\u81ea\u52a8\u5339\u914d");

        // ════════════════════════════════════════════════════════
        //  传感器提供者切换点
        // ════════════════════════════════════════════════════════
        ISensorProvider sensorProvider;

#if USE_LIBRE_HARDWARE_MONITOR
        if (settings.UseLhm)
        {
            _logger.Info("Plugin", "\u4f7f\u7528 LibreHardwareMonitor \u771f\u5b9e\u4f20\u611f\u5668");
            sensorProvider = new LibreHardwareMonitorSensorProvider(bindingConfig, _logger);
        }
        else
        {
            _logger.Info("Plugin", "\u914d\u7f6e\u4e3a mock \u6a21\u5f0f\uff0c\u4f7f\u7528\u6a21\u62df\u4f20\u611f\u5668");
            sensorProvider = new MockSensorProvider(bindingConfig, _logger);
        }
#else
        if (settings.UseLhm)
        {
            _logger.Warn("Plugin", "\u914d\u7f6e\u8981\u6c42\u4f7f\u7528 LHM \u4f20\u611f\u5668\uff0c\u4f46\u7f16\u8bd1\u65f6\u672a\u542f\u7528 USE_LHM");
            _logger.Warn("Plugin", "\u8bf7\u4f7f\u7528 dotnet build -p:USE_LHM=true \u91cd\u65b0\u7f16\u8bd1");
            _logger.Warn("Plugin", "\u5f53\u524d\u56de\u9000\u5230\u6a21\u62df\u4f20\u611f\u5668");
        }
        else
        {
            _logger.Info("Plugin", "\u4f7f\u7528\u6a21\u62df\u4f20\u611f\u5668 (MockSensorProvider)");
        }
        sensorProvider = new MockSensorProvider(bindingConfig, _logger);
#endif

        sensorProvider.Initialize();
        _logger.Info("Plugin", $"传感器提供者初始化完成: {sensorProvider.GetType().Name}");

        // 创建适配器（核心引擎）
        _adapter = new FanControlPluginAdapter(settings, sensorProvider, _logger);

        // ── 注册温度传感器 ──
        _cpuTempSensor = new AiTempSensor("AI-CPU-Temp", "ai_cpu_temp", () =>
            (float?)_adapter.LastSnapshot?.CpuTemperature);
        _gpuTempSensor = new AiTempSensor("AI-GPU-Temp", "ai_gpu_temp", () =>
            (float?)_adapter.LastSnapshot?.GpuTemperature);
        _mbTempSensor = new AiTempSensor("AI-MB-Temp", "ai_mb_temp", () =>
            (float?)_adapter.LastSnapshot?.MotherboardTemperature);

        _container.TempSensors.Add(_cpuTempSensor);
        _container.TempSensors.Add(_gpuTempSensor);
        _container.TempSensors.Add(_mbTempSensor);

        _logger.Debug("Plugin", "已注册 3 个温度传感器: AI-CPU-Temp, AI-GPU-Temp, AI-MB-Temp");

        // ── 注册控制传感器（三路风扇输出） ──
        _cpuFanCtrl = new AiControlSensor("AI-CPU-Fan", "ai_cpu_fan", () =>
            (float?)_adapter.LastDecision?.CpuFanPercent);
        _gpuFanCtrl = new AiControlSensor("AI-GPU-Fan", "ai_gpu_fan", () =>
            (float?)_adapter.LastDecision?.GpuFanPercent);
        _caseFanCtrl = new AiControlSensor("AI-Case-Fan", "ai_case_fan", () =>
            (float?)_adapter.LastDecision?.CaseFanPercent);

        _container.ControlSensors.Add(_cpuFanCtrl);
        _container.ControlSensors.Add(_gpuFanCtrl);
        _container.ControlSensors.Add(_caseFanCtrl);

        _logger.Debug("Plugin", "已注册 3 个控制传感器: AI-CPU-Fan, AI-GPU-Fan, AI-Case-Fan");
        _logger.Info("Plugin", "========== AiFanPlugin.Load() 完成 ==========");
    }

    /// <summary>
    /// FanControl 每个轮询周期调用（IPlugin2 新增）。
    /// 驱动适配器执行一次传感器采集 + AI 决策（按 pollingIntervalSeconds 节流）。
    /// </summary>
    public void Update()
    {
        _adapter?.TickOnce();
    }

    /// <summary>
    /// FanControl 关闭或重新加载时调用。
    /// </summary>
    public void Close()
    {
        _logger?.Info("Plugin", "AiFanPlugin.Close() — 插件正在关闭");

        _adapter?.Dispose();
        _adapter = null;

        _logger?.Info("Plugin", "插件已关闭");
        _logger?.Dispose();
        _logger = null;
    }
}
