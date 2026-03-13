using FanControl.AiPlugin.Config;
using FanControl.AiPlugin.Logging;
using FanControl.AiPlugin.Models;

namespace FanControl.AiPlugin.Sensors;

/// <summary>
/// 模拟传感器数据提供者。
/// 生成带有一定真实感的随机数据，用于在没有硬件传感器的环境下演示。
/// 真实插件中，替换为 LibreHardwareMonitorSensorProvider 或从 FanControl 内部读取。
/// </summary>
public sealed class MockSensorProvider : ISensorProvider
{
    private readonly Random _rng = new();
    private readonly SensorBindingConfig _bindingConfig;
    private readonly PluginLogger? _logger;

    // 各传感器的"基准值"，每次采集在基准上小幅波动
    private double _cpuTempBase;
    private double _gpuTempBase;
    private double _mbTempBase;

    public MockSensorProvider(SensorBindingConfig? bindingConfig = null, PluginLogger? logger = null)
    {
        _bindingConfig = bindingConfig ?? new SensorBindingConfig();
        _logger = logger;
    }

    public void Initialize()
    {
        _cpuTempBase = 50 + _rng.NextDouble() * 20;   // 50~70
        _gpuTempBase = 45 + _rng.NextDouble() * 25;   // 45~70
        _mbTempBase  = 30 + _rng.NextDouble() * 10;   // 30~40
        Console.WriteLine("  [MockSensorProvider] \u6a21\u62df\u4f20\u611f\u5668\u5df2\u521d\u59cb\u5316");

        if (_bindingConfig.HasAnyBinding)
        {
            Console.WriteLine("  [MockSensorProvider] \u68c0\u6d4b\u5230\u7528\u6237\u4f20\u611f\u5668\u7ed1\u5b9a\u914d\u7f6e\uff08\u6a21\u62df\u6a21\u5f0f\u4e0b\u5ffd\u7565\uff0c\u4ec5 LHM \u6a21\u5f0f\u751f\u6548\uff09");
            _logger?.Info("MockSensor", "\u7528\u6237\u914d\u7f6e\u4e86\u4f20\u611f\u5668\u7ed1\u5b9a\uff0c\u4f46\u6a21\u62df\u6a21\u5f0f\u4e0d\u4f7f\u7528\u771f\u5b9e\u4f20\u611f\u5668\uff0c\u7ed1\u5b9a\u914d\u7f6e\u5c06\u88ab\u5ffd\u7565");
        }
    }

    public FanRuntimeSnapshot Collect(FanRuntimeSnapshot? previous)
    {
        // 在基准值上小幅随机漂移（±3°C）
        _cpuTempBase = Drift(_cpuTempBase, 35, 95, 3);
        _gpuTempBase = Drift(_gpuTempBase, 30, 92, 3);
        _mbTempBase  = Drift(_mbTempBase,  25, 55, 2);

        var now = DateTime.UtcNow;

        var snapshot = new FanRuntimeSnapshot
        {
            CpuTemperature          = Round(_cpuTempBase),
            GpuTemperature          = Round(_gpuTempBase),
            MotherboardTemperature  = Round(_mbTempBase),
            CpuUsagePercent         = Round(_rng.NextDouble() * 100),
            GpuUsagePercent         = Round(_rng.NextDouble() * 100),
            CurrentCpuFanPercent    = Round(30 + _rng.NextDouble() * 50),
            CurrentGpuFanPercent    = Round(25 + _rng.NextDouble() * 55),
            CurrentCaseFanPercent   = Round(20 + _rng.NextDouble() * 40),
            TimestampUtc            = now
        };

        // 计算温度趋势（°C/min）
        if (previous is not null)
        {
            var minutes = (now - previous.TimestampUtc).TotalMinutes;
            if (minutes > 0.001)
            {
                snapshot.CpuTempTrend         = Round((snapshot.CpuTemperature - previous.CpuTemperature) / minutes);
                snapshot.GpuTempTrend         = Round((snapshot.GpuTemperature - previous.GpuTemperature) / minutes);
                snapshot.MotherboardTempTrend  = Round((snapshot.MotherboardTemperature - previous.MotherboardTemperature) / minutes);
            }
        }

        return snapshot;
    }

    /// <summary>让基准值在 [min, max] 范围内随机漂移</summary>
    private double Drift(double current, double min, double max, double range)
    {
        var delta = (_rng.NextDouble() * 2 - 1) * range;
        return Math.Clamp(current + delta, min, max);
    }

    private static double Round(double v) => Math.Round(v, 1);

    public void Dispose() { /* 模拟实现无需释放资源 */ }
}
