#if USE_LIBRE_HARDWARE_MONITOR
using LibreHardwareMonitor.Hardware;
using FanControl.AiPlugin.Config;
using FanControl.AiPlugin.Logging;
using FanControl.AiPlugin.Models;

namespace FanControl.AiPlugin.Sensors;

public sealed class LibreHardwareMonitorSensorProvider : ISensorProvider
{
    private Computer? _computer;
    private readonly SensorBindingConfig _bindingConfig;
    private readonly PluginLogger? _logger;
    private ISensor? _cpuTempSensor;
    private ISensor? _gpuTempSensor;
    private ISensor? _mbTempSensor;
    private ISensor? _cpuLoadSensor;
    private ISensor? _gpuLoadSensor;
    private ISensor? _cpuFanSensor;
    private ISensor? _gpuFanSensor;
    private ISensor? _caseFanSensor;
    private readonly List<SensorBindingResult> _bindingResults = [];
    private const float DefaultMaxFanRpm = 2000f;

    public LibreHardwareMonitorSensorProvider(SensorBindingConfig? bindingConfig = null, PluginLogger? logger = null)
    {
        _bindingConfig = bindingConfig ?? new SensorBindingConfig();
        _logger = logger;
    }

    public void Initialize()
    {
        Console.WriteLine("  [LHM] 正在初始化 LibreHardwareMonitor...");
        _logger?.Info("LHM", "正在初始化 LibreHardwareMonitor...");

        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true
        };

        _computer.Open();

        var allTempSensors = new List<(IHardware Hw, ISensor Sensor)>();
        var allLoadSensors = new List<(IHardware Hw, ISensor Sensor)>();
        var allFanSensors = new List<(IHardware Hw, ISensor Sensor)>();

        foreach (var hw in _computer.Hardware)
        {
            hw.Update();
            foreach (var sub in hw.SubHardware)
            {
                sub.Update();
                CollectSensors(sub, allTempSensors, allLoadSensors, allFanSensors);
            }
            CollectSensors(hw, allTempSensors, allLoadSensors, allFanSensors);
        }

        BindCpuTemp(allTempSensors);
        BindGpuTemp(allTempSensors);
        BindMbTemp(allTempSensors);
        BindLoadSensors(allLoadSensors);
        BindFanSensors(allFanSensors);
        PrintDiscoveryResult();
    }

    private static void CollectSensors(IHardware hw, List<(IHardware, ISensor)> temps, List<(IHardware, ISensor)> loads, List<(IHardware, ISensor)> fans)
    {
        foreach (var sensor in hw.Sensors)
        {
            switch (sensor.SensorType)
            {
                case SensorType.Temperature: temps.Add((hw, sensor)); break;
                case SensorType.Load: loads.Add((hw, sensor)); break;
                case SensorType.Fan: fans.Add((hw, sensor)); break;
            }
        }
    }

    private void BindCpuTemp(List<(IHardware Hw, ISensor Sensor)> temps)
    {
        var userName = _bindingConfig.CpuSensorName;
        var result = TryBindByUserName(userName, temps, out _cpuTempSensor);
        if (_cpuTempSensor is null)
        {
            _cpuTempSensor = temps.Where(t => t.Hw.HardwareType == HardwareType.Cpu).Select(t => t.Sensor).FirstOrDefault(s =>
            {
                var n = s.Name.ToLowerInvariant();
                return n.Contains("package") || n.Contains("tctl") || n.Contains("tdie") || n.Contains("cpu");
            });
            _bindingResults.Add(new SensorBindingResult { Role = "CPU 温度", UserSpecifiedName = userName, BoundSensorName = _cpuTempSensor?.Name ?? string.Empty, MatchedByUserName = false, FellBackToAuto = !string.IsNullOrWhiteSpace(userName) });
        }
        else
        {
            _bindingResults.Add(result with { Role = "CPU 温度" });
        }
    }

    private void BindGpuTemp(List<(IHardware Hw, ISensor Sensor)> temps)
    {
        var userName = _bindingConfig.GpuSensorName;
        var result = TryBindByUserName(userName, temps, out _gpuTempSensor);
        if (_gpuTempSensor is null)
        {
            _gpuTempSensor = temps.Where(t => IsGpu(t.Hw.HardwareType)).Select(t => t.Sensor).FirstOrDefault(s =>
            {
                var n = s.Name.ToLowerInvariant();
                return n.Contains("gpu") || n.Contains("core") || n.Contains("hot spot");
            });
            _bindingResults.Add(new SensorBindingResult { Role = "GPU 温度", UserSpecifiedName = userName, BoundSensorName = _gpuTempSensor?.Name ?? string.Empty, MatchedByUserName = false, FellBackToAuto = !string.IsNullOrWhiteSpace(userName) });
        }
        else
        {
            _bindingResults.Add(result with { Role = "GPU 温度" });
        }
    }

    private void BindMbTemp(List<(IHardware Hw, ISensor Sensor)> temps)
    {
        var userName = _bindingConfig.MotherboardSensorName;
        var result = TryBindByUserName(userName, temps, out _mbTempSensor);
        if (_mbTempSensor is null)
        {
            _mbTempSensor = temps.Where(t => t.Hw.HardwareType == HardwareType.Motherboard).Select(t => t.Sensor).FirstOrDefault();
            _bindingResults.Add(new SensorBindingResult { Role = "主板温度", UserSpecifiedName = userName, BoundSensorName = _mbTempSensor?.Name ?? string.Empty, MatchedByUserName = false, FellBackToAuto = !string.IsNullOrWhiteSpace(userName) });
        }
        else
        {
            _bindingResults.Add(result with { Role = "主板温度" });
        }
    }

    private SensorBindingResult TryBindByUserName(string userName, List<(IHardware Hw, ISensor Sensor)> sensors, out ISensor? bound)
    {
        bound = null;
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new SensorBindingResult { UserSpecifiedName = string.Empty, MatchedByUserName = false, FellBackToAuto = false };
        }

        _logger?.Debug("LHM", $"尝试按用户指定名称绑定: \"{userName}\" (模式={(_bindingConfig.UseExactMatch ? "精确" : "模糊")})");
        foreach (var (_, sensor) in sensors)
        {
            if (NameMatches(sensor.Name, userName))
            {
                bound = sensor;
                _logger?.Info("LHM", $"用户指定传感器命中: \"{userName}\" -> {sensor.Name}");
                return new SensorBindingResult { UserSpecifiedName = userName, BoundSensorName = sensor.Name, MatchedByUserName = true, FellBackToAuto = false };
            }
        }

        _logger?.Warn("LHM", $"用户指定传感器未命中: \"{userName}\"，将回退到自动匹配");
        return new SensorBindingResult { UserSpecifiedName = userName, MatchedByUserName = false, FellBackToAuto = true };
    }

    private bool NameMatches(string sensorName, string pattern)
    {
        if (_bindingConfig.UseExactMatch)
            return string.Equals(sensorName, pattern, StringComparison.OrdinalIgnoreCase);
        return sensorName.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private void BindLoadSensors(List<(IHardware Hw, ISensor Sensor)> loads)
    {
        _cpuLoadSensor = loads.Where(t => t.Hw.HardwareType == HardwareType.Cpu).Select(t => t.Sensor).FirstOrDefault(s =>
        {
            var n = s.Name.ToLowerInvariant();
            return n.Contains("total") || n.Contains("cpu");
        });

        _gpuLoadSensor = loads.Where(t => IsGpu(t.Hw.HardwareType)).Select(t => t.Sensor).FirstOrDefault(s =>
        {
            var n = s.Name.ToLowerInvariant();
            return n.Contains("core") || n.Contains("gpu");
        });
    }

    private void BindFanSensors(List<(IHardware Hw, ISensor Sensor)> fans)
    {
        _cpuFanSensor = fans.Select(t => t.Sensor).FirstOrDefault(s =>
        {
            var n = s.Name.ToLowerInvariant();
            return n.Contains("cpu") || n.Contains("#1");
        });

        _gpuFanSensor = fans.Where(t => IsGpu(t.Hw.HardwareType)).Select(t => t.Sensor).FirstOrDefault();
        _caseFanSensor = fans.Select(t => t.Sensor).Where(s => s != _cpuFanSensor && s != _gpuFanSensor).FirstOrDefault(s =>
        {
            var n = s.Name.ToLowerInvariant();
            return n.Contains("chassis") || n.Contains("sys") || n.Contains("case") || n.Contains("#2");
        });
    }

    private static bool IsGpu(HardwareType type) => type == HardwareType.GpuNvidia || type == HardwareType.GpuAmd || type == HardwareType.GpuIntel;

    private void PrintDiscoveryResult()
    {
        foreach (var r in _bindingResults)
            _logger?.Info("LHM", $"绑定: {r}");
    }

    public List<string> GetBindingStatus()
    {
        var result = new List<string>();
        foreach (var r in _bindingResults)
            result.Add(r.ToString());
        return result;
    }

    public List<SensorBindingResult> GetBindingResults() => [.. _bindingResults];

    public FanRuntimeSnapshot Collect(FanRuntimeSnapshot? previous)
    {
        if (_computer is null)
            throw new InvalidOperationException("传感器未初始化，请先调用 Initialize()");

        foreach (var hw in _computer.Hardware)
        {
            hw.Update();
            foreach (var sub in hw.SubHardware)
                sub.Update();
        }

        var now = DateTime.UtcNow;
        var snapshot = new FanRuntimeSnapshot
        {
            CpuTemperature = ReadValue(_cpuTempSensor),
            GpuTemperature = ReadValue(_gpuTempSensor),
            MotherboardTemperature = ReadValue(_mbTempSensor),
            CpuUsagePercent = ReadValue(_cpuLoadSensor),
            GpuUsagePercent = ReadValue(_gpuLoadSensor),
            CurrentCpuFanPercent = FanRpmToPercent(_cpuFanSensor),
            CurrentGpuFanPercent = FanRpmToPercent(_gpuFanSensor),
            CurrentCaseFanPercent = FanRpmToPercent(_caseFanSensor),
            TimestampUtc = now
        };

        if (previous is not null)
        {
            var minutes = (now - previous.TimestampUtc).TotalMinutes;
            if (minutes > 0.001)
            {
                snapshot.CpuTempTrend = Math.Round((snapshot.CpuTemperature - previous.CpuTemperature) / minutes, 1);
                snapshot.GpuTempTrend = Math.Round((snapshot.GpuTemperature - previous.GpuTemperature) / minutes, 1);
                snapshot.MotherboardTempTrend = Math.Round((snapshot.MotherboardTemperature - previous.MotherboardTemperature) / minutes, 1);
            }
        }

        return snapshot;
    }

    private static double ReadValue(ISensor? sensor) => Math.Round(sensor?.Value ?? 0f, 1);

    private static double FanRpmToPercent(ISensor? sensor)
    {
        if (sensor is null) return 0;
        var rpm = sensor.Value ?? 0f;
        if (rpm <= 0) return 0;
        var pct = rpm / DefaultMaxFanRpm * 100f;
        return Math.Round(Math.Clamp(pct, 0, 100), 1);
    }

    public void Dispose()
    {
        if (_computer is not null)
        {
            _computer.Close();
            _computer = null;
        }
    }
}
#endif
