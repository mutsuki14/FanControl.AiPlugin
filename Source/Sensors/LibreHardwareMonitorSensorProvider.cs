#if USE_LIBRE_HARDWARE_MONITOR
using LibreHardwareMonitor.Hardware;
using FanControl.AiPlugin.Config;
using FanControl.AiPlugin.Logging;
using FanControl.AiPlugin.Models;

namespace FanControl.AiPlugin.Sensors;

/// <summary>
/// 基于 LibreHardwareMonitor 的真实硬件传感器提供者。
/// 支持用户自定义传感器名称绑定（优先）和自动匹配（回退）。
///
/// 使用前提：
///   1. 以管理员权限运行（某些传感器需要管理员权限）
///   2. 编译时启用 USE_LHM=true
///   3. 配置文件 sensorProvider 设为 "lhm"
///
/// 注意：此类仅在条件编译符号 USE_LIBRE_HARDWARE_MONITOR 定义时参与编译。
/// </summary>
public sealed class LibreHardwareMonitorSensorProvider : ISensorProvider
{
    private Computer? _computer;
    private readonly SensorBindingConfig _bindingConfig;
    private readonly PluginLogger? _logger;

    // 缓存找到的传感器引用，避免每次 Collect 都重新遍历
    private ISensor? _cpuTempSensor;
    private ISensor? _gpuTempSensor;
    private ISensor? _mbTempSensor;
    private ISensor? _cpuLoadSensor;
    private ISensor? _gpuLoadSensor;
    private ISensor? _cpuFanSensor;
    private ISensor? _gpuFanSensor;
    private ISensor? _caseFanSensor;

    // 绑定结果（供诊断使用）
    private readonly List<SensorBindingResult> _bindingResults = [];

    // 风扇 RPM → 百分比的估算参考值（典型最大转速）
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
            IsControllerEnabled = true,
            IsFanControllerEnabled = true
        };

        _computer.Open();

        // 收集所有可用的温度传感器用于匹配
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

        // 打印所有发现的温度传感器（帮助用户确定名称）
        if (_logger?.Enabled == true)
        {
            _logger.Debug("LHM", $"发现 {allTempSensors.Count} 个温度传感器:");
            foreach (var (hw, s) in allTempSensors)
                _logger.Debug("LHM", $"  [{hw.HardwareType}] {s.Name} = {s.Value?.ToString("F1") ?? "N/A"}");
        }

        // ── 绑定温度传感器（用户指定 → 自动匹配） ──
        BindCpuTemp(allTempSensors);
        BindGpuTemp(allTempSensors);
        BindMbTemp(allTempSensors);

        // ── 绑定负载传感器（仅自动匹配） ──
        BindLoadSensors(allLoadSensors);

        // ── 绑定风扇传感器（仅自动匹配） ──
        BindFanSensors(allFanSensors);

        PrintDiscoveryResult();
    }

    /// <summary>收集所有传感器到分类列表</summary>
    private static void CollectSensors(IHardware hw,
        List<(IHardware, ISensor)> temps,
        List<(IHardware, ISensor)> loads,
        List<(IHardware, ISensor)> fans)
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

    // ═══════════════════════════════════════════════════════════
    //  温度传感器绑定：用户指定优先，回退到自动匹配
    // ═══════════════════════════════════════════════════════════

    private void BindCpuTemp(List<(IHardware Hw, ISensor Sensor)> temps)
    {
        var userName = _bindingConfig.CpuSensorName;
        var result = TryBindByUserName(userName, temps, out _cpuTempSensor);

        if (_cpuTempSensor is null)
        {
            // 自动匹配：CPU 硬件中 package/tctl/tdie/cpu 关键词
            _cpuTempSensor = temps
                .Where(t => t.Hw.HardwareType == HardwareType.Cpu)
                .Select(t => t.Sensor)
                .FirstOrDefault(s =>
                {
                    var n = s.Name.ToLowerInvariant();
                    return n.Contains("package") || n.Contains("tctl") || n.Contains("tdie") || n.Contains("cpu");
                });

            _bindingResults.Add(new SensorBindingResult
            {
                Role = "CPU 温度",
                UserSpecifiedName = userName,
                BoundSensorName = _cpuTempSensor?.Name ?? string.Empty,
                MatchedByUserName = false,
                FellBackToAuto = !string.IsNullOrWhiteSpace(userName)
            });
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
            _gpuTempSensor = temps
                .Where(t => IsGpu(t.Hw.HardwareType))
                .Select(t => t.Sensor)
                .FirstOrDefault(s =>
                {
                    var n = s.Name.ToLowerInvariant();
                    return n.Contains("gpu") || n.Contains("core") || n.Contains("hot spot");
                });

            _bindingResults.Add(new SensorBindingResult
            {
                Role = "GPU 温度",
                UserSpecifiedName = userName,
                BoundSensorName = _gpuTempSensor?.Name ?? string.Empty,
                MatchedByUserName = false,
                FellBackToAuto = !string.IsNullOrWhiteSpace(userName)
            });
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
            _mbTempSensor = temps
                .Where(t => t.Hw.HardwareType == HardwareType.Motherboard)
                .Select(t => t.Sensor)
                .FirstOrDefault();

            _bindingResults.Add(new SensorBindingResult
            {
                Role = "主板温度",
                UserSpecifiedName = userName,
                BoundSensorName = _mbTempSensor?.Name ?? string.Empty,
                MatchedByUserName = false,
                FellBackToAuto = !string.IsNullOrWhiteSpace(userName)
            });
        }
        else
        {
            _bindingResults.Add(result with { Role = "主板温度" });
        }
    }

    /// <summary>
    /// 尝试按用户指定名称绑定传感器。
    /// 如果名称为空，直接返回 null（未尝试）。
    /// </summary>
    private SensorBindingResult TryBindByUserName(string userName,
        List<(IHardware Hw, ISensor Sensor)> sensors, out ISensor? bound)
    {
        bound = null;

        if (string.IsNullOrWhiteSpace(userName))
        {
            return new SensorBindingResult
            {
                UserSpecifiedName = string.Empty,
                MatchedByUserName = false,
                FellBackToAuto = false
            };
        }

        _logger?.Debug("LHM", $"尝试按用户指定名称绑定: \"{userName}\" (模式={(_bindingConfig.UseExactMatch ? "精确" : "模糊")})");

        foreach (var (_, sensor) in sensors)
        {
            if (NameMatches(sensor.Name, userName))
            {
                bound = sensor;
                _logger?.Info("LHM", $"用户指定传感器命中: \"{userName}\" -> {sensor.Name}");
                return new SensorBindingResult
                {
                    UserSpecifiedName = userName,
                    BoundSensorName = sensor.Name,
                    MatchedByUserName = true,
                    FellBackToAuto = false
                };
            }
        }

        _logger?.Warn("LHM", $"用户指定传感器未命中: \"{userName}\"，将回退到自动匹配");
        return new SensorBindingResult
        {
            UserSpecifiedName = userName,
            MatchedByUserName = false,
            FellBackToAuto = true
        };
    }

    /// <summary>名称匹配：精确匹配或模糊包含（不区分大小写）</summary>
    private bool NameMatches(string sensorName, string pattern)
    {
        if (_bindingConfig.UseExactMatch)
            return string.Equals(sensorName, pattern, StringComparison.OrdinalIgnoreCase);

        return sensorName.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }

    // ═══════════════════════════════════════════════════════════
    //  负载/风扇传感器绑定（仅自动匹配，不支持用户指定）
    // ═══════════════════════════════════════════════════════════

    private void BindLoadSensors(List<(IHardware Hw, ISensor Sensor)> loads)
    {
        _cpuLoadSensor = loads
            .Where(t => t.Hw.HardwareType == HardwareType.Cpu)
            .Select(t => t.Sensor)
            .FirstOrDefault(s =>
            {
                var n = s.Name.ToLowerInvariant();
                return n.Contains("total") || n.Contains("cpu");
            });

        _gpuLoadSensor = loads
            .Where(t => IsGpu(t.Hw.HardwareType))
            .Select(t => t.Sensor)
            .FirstOrDefault(s =>
            {
                var n = s.Name.ToLowerInvariant();
                return n.Contains("core") || n.Contains("gpu");
            });
    }

    private void BindFanSensors(List<(IHardware Hw, ISensor Sensor)> fans)
    {
        _cpuFanSensor = fans
            .Select(t => t.Sensor)
            .FirstOrDefault(s =>
            {
                var n = s.Name.ToLowerInvariant();
                return n.Contains("cpu") || n.Contains("#1");
            });

        _gpuFanSensor = fans
            .Where(t => IsGpu(t.Hw.HardwareType))
            .Select(t => t.Sensor)
            .FirstOrDefault();

        _caseFanSensor = fans
            .Select(t => t.Sensor)
            .Where(s => s != _cpuFanSensor && s != _gpuFanSensor)
            .FirstOrDefault(s =>
            {
                var n = s.Name.ToLowerInvariant();
                return n.Contains("chassis") || n.Contains("sys") || n.Contains("case") || n.Contains("#2");
            });
    }

    /// <summary>判断硬件类型是否为 GPU</summary>
    private static bool IsGpu(HardwareType type) =>
        type == HardwareType.GpuNvidia || type == HardwareType.GpuAmd || type == HardwareType.GpuIntel;

    /// <summary>打印传感器发现结果</summary>
    private void PrintDiscoveryResult()
    {
        Console.WriteLine("  [LHM] 传感器绑定结果：");
        foreach (var r in _bindingResults)
            Console.WriteLine($"    {r}");

        Console.WriteLine($"    CPU 负载:  {FormatSensor(_cpuLoadSensor)}");
        Console.WriteLine($"    GPU 负载:  {FormatSensor(_gpuLoadSensor)}");
        Console.WriteLine($"    CPU 风扇:  {FormatSensor(_cpuFanSensor)}");
        Console.WriteLine($"    GPU 风扇:  {FormatSensor(_gpuFanSensor)}");
        Console.WriteLine($"    机箱风扇:  {FormatSensor(_caseFanSensor)}");

        var bound = new ISensor?[] { _cpuTempSensor, _gpuTempSensor, _mbTempSensor, _cpuLoadSensor, _gpuLoadSensor }
            .Count(s => s is not null);
        Console.WriteLine($"  [LHM] 已绑定 {bound}/5 个核心传感器（温度+负载）");

        if (_cpuTempSensor is null)
            Console.WriteLine("  [LHM] 未找到 CPU 温度传感器，将返回 0°C");
        if (_gpuTempSensor is null)
            Console.WriteLine("  [LHM] 未找到 GPU 温度传感器，将返回 0°C");

        // 日志输出绑定结果
        foreach (var r in _bindingResults)
            _logger?.Info("LHM", $"绑定: {r}");
    }

    /// <summary>获取传感器绑定状态列表（供诊断摘要使用）</summary>
    public List<string> GetBindingStatus()
    {
        var result = new List<string>();
        foreach (var r in _bindingResults)
            result.Add(r.ToString());

        result.Add($"CPU 负载:  {FormatSensor(_cpuLoadSensor)}");
        result.Add($"GPU 负载:  {FormatSensor(_gpuLoadSensor)}");
        result.Add($"CPU 风扇:  {FormatSensor(_cpuFanSensor)}");
        result.Add($"GPU 风扇:  {FormatSensor(_gpuFanSensor)}");
        result.Add($"机箱风扇:  {FormatSensor(_caseFanSensor)}");
        return result;
    }

    /// <summary>获取详细绑定结果（供诊断摘要使用）</summary>
    public List<SensorBindingResult> GetBindingResults() => [.. _bindingResults];

    private static string FormatSensor(ISensor? sensor) =>
        sensor is null ? "(未找到)" : $"{sensor.Name} = {sensor.Value?.ToString("F1") ?? "N/A"}";

    public FanRuntimeSnapshot Collect(FanRuntimeSnapshot? previous)
    {
        if (_computer is null)
            throw new InvalidOperationException("传感器未初始化，请先调用 Initialize()");

        // 刷新所有硬件数据
        foreach (var hw in _computer.Hardware)
        {
            hw.Update();
            foreach (var sub in hw.SubHardware)
                sub.Update();
        }

        var now = DateTime.UtcNow;

        var snapshot = new FanRuntimeSnapshot
        {
            CpuTemperature          = ReadValue(_cpuTempSensor),
            GpuTemperature          = ReadValue(_gpuTempSensor),
            MotherboardTemperature  = ReadValue(_mbTempSensor),
            CpuUsagePercent         = ReadValue(_cpuLoadSensor),
            GpuUsagePercent         = ReadValue(_gpuLoadSensor),
            CurrentCpuFanPercent    = FanRpmToPercent(_cpuFanSensor),
            CurrentGpuFanPercent    = FanRpmToPercent(_gpuFanSensor),
            CurrentCaseFanPercent   = FanRpmToPercent(_caseFanSensor),
            TimestampUtc            = now
        };

        // 计算温度趋势（°C/min）
        if (previous is not null)
        {
            var minutes = (now - previous.TimestampUtc).TotalMinutes;
            if (minutes > 0.001)
            {
                snapshot.CpuTempTrend         = Math.Round((snapshot.CpuTemperature - previous.CpuTemperature) / minutes, 1);
                snapshot.GpuTempTrend         = Math.Round((snapshot.GpuTemperature - previous.GpuTemperature) / minutes, 1);
                snapshot.MotherboardTempTrend  = Math.Round((snapshot.MotherboardTemperature - previous.MotherboardTemperature) / minutes, 1);
            }
        }

        return snapshot;
    }

    /// <summary>安全读取传感器值，传感器为 null 或值为 null 时返回 0</summary>
    private static double ReadValue(ISensor? sensor) =>
        Math.Round(sensor?.Value ?? 0f, 1);

    /// <summary>
    /// 将风扇 RPM 值估算为百分比（0~100）。
    /// </summary>
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
            Console.WriteLine("  [LHM] 正在关闭 LibreHardwareMonitor...");
            _computer.Close();
            _computer = null;
        }
    }
}
#endif
