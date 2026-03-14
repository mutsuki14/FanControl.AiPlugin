using FanControl.AiPlugin.Logging;
using FanControl.AiPlugin.Models;

namespace FanControl.AiPlugin.Services;

/// <summary>
/// 传感器数据清洗：过滤异常值、检测跳变、用上次已知好值回退。
/// 在传感器采集后、AI 决策前执行，确保输入数据质量。
/// </summary>
public static class SensorSanitizer
{
    // ── 合理范围 ──
    private const double MinTemp = -10.0;       // °C，低于此视为异常
    private const double MaxTemp = 130.0;        // °C，高于此视为异常
    private const double MinLoad = 0.0;          // %
    private const double MaxLoad = 100.0;        // %
    private const double MinFanPercent = 0.0;    // %
    private const double MaxFanPercent = 100.0;  // %

    // ── 跳变检测 ──
    private const double TempSpikeThreshold = 30.0;  // °C，单次采样温度跳变超过此值视为异常
    private const double LoadSpikeThreshold = 60.0;   // %，单次采样负载跳变超过此值视为异常

    /// <summary>
    /// 清洗快照数据。检测范围异常和跳变异常，用上次已知好值替换。
    /// 返回清洗后的快照（新对象，不修改原始快照）。
    /// </summary>
    public static FanRuntimeSnapshot Sanitize(
        FanRuntimeSnapshot raw,
        FanRuntimeSnapshot? lastGood,
        PluginLogger? logger = null)
    {
        var result = new FanRuntimeSnapshot
        {
            CpuTemperature          = raw.CpuTemperature,
            GpuTemperature          = raw.GpuTemperature,
            MotherboardTemperature  = raw.MotherboardTemperature,
            CpuUsagePercent         = raw.CpuUsagePercent,
            GpuUsagePercent         = raw.GpuUsagePercent,
            CurrentCpuFanPercent    = raw.CurrentCpuFanPercent,
            CurrentGpuFanPercent    = raw.CurrentGpuFanPercent,
            CurrentCaseFanPercent   = raw.CurrentCaseFanPercent,
            CpuTempTrend            = raw.CpuTempTrend,
            GpuTempTrend            = raw.GpuTempTrend,
            MotherboardTempTrend    = raw.MotherboardTempTrend,
            TimestampUtc            = raw.TimestampUtc
        };

        var anomalyCount = 0;

        // ── 温度范围检查 ──
        var cpuTemp = result.CpuTemperature;
        var gpuTemp = result.GpuTemperature;
        var mbTemp = result.MotherboardTemperature;
        anomalyCount += SanitizeTemp(ref cpuTemp, lastGood?.CpuTemperature, "CPU温度", logger);
        anomalyCount += SanitizeTemp(ref gpuTemp, lastGood?.GpuTemperature, "GPU温度", logger);
        anomalyCount += SanitizeTemp(ref mbTemp, lastGood?.MotherboardTemperature, "主板温度", logger);
        result.CpuTemperature = cpuTemp;
        result.GpuTemperature = gpuTemp;
        result.MotherboardTemperature = mbTemp;

        // ── 负载范围检查 ──
        var cpuUsage = result.CpuUsagePercent;
        var gpuUsage = result.GpuUsagePercent;
        anomalyCount += SanitizeLoad(ref cpuUsage, lastGood?.CpuUsagePercent, "CPU负载", logger);
        anomalyCount += SanitizeLoad(ref gpuUsage, lastGood?.GpuUsagePercent, "GPU负载", logger);
        result.CpuUsagePercent = cpuUsage;
        result.GpuUsagePercent = gpuUsage;

        // ── 风扇范围检查 ──
        var cpuFan = result.CurrentCpuFanPercent;
        var gpuFan = result.CurrentGpuFanPercent;
        var caseFan = result.CurrentCaseFanPercent;
        anomalyCount += SanitizeFan(ref cpuFan, lastGood?.CurrentCpuFanPercent, "CPU风扇", logger);
        anomalyCount += SanitizeFan(ref gpuFan, lastGood?.CurrentGpuFanPercent, "GPU风扇", logger);
        anomalyCount += SanitizeFan(ref caseFan, lastGood?.CurrentCaseFanPercent, "机箱风扇", logger);
        result.CurrentCpuFanPercent = cpuFan;
        result.CurrentGpuFanPercent = gpuFan;
        result.CurrentCaseFanPercent = caseFan;

        // ── 跳变检测（仅在有上次好值时）
        if (lastGood is not null)
        {
            cpuTemp = result.CpuTemperature;
            gpuTemp = result.GpuTemperature;
            mbTemp = result.MotherboardTemperature;
            cpuUsage = result.CpuUsagePercent;
            gpuUsage = result.GpuUsagePercent;

            anomalyCount += DetectSpike(ref cpuTemp, lastGood.CpuTemperature, TempSpikeThreshold, "CPU温度", logger);
            anomalyCount += DetectSpike(ref gpuTemp, lastGood.GpuTemperature, TempSpikeThreshold, "GPU温度", logger);
            anomalyCount += DetectSpike(ref mbTemp, lastGood.MotherboardTemperature, TempSpikeThreshold, "主板温度", logger);
            anomalyCount += DetectSpike(ref cpuUsage, lastGood.CpuUsagePercent, LoadSpikeThreshold, "CPU负载", logger);
            anomalyCount += DetectSpike(ref gpuUsage, lastGood.GpuUsagePercent, LoadSpikeThreshold, "GPU负载", logger);

            result.CpuTemperature = cpuTemp;
            result.GpuTemperature = gpuTemp;
            result.MotherboardTemperature = mbTemp;
            result.CpuUsagePercent = cpuUsage;
            result.GpuUsagePercent = gpuUsage;
        }

        // ── 趋势值合理性（限制在 ±50 °C/min 内）
        result.CpuTempTrend = Math.Clamp(result.CpuTempTrend, -50, 50);
        result.GpuTempTrend = Math.Clamp(result.GpuTempTrend, -50, 50);
        result.MotherboardTempTrend = Math.Clamp(result.MotherboardTempTrend, -50, 50);

        if (anomalyCount > 0)
            logger?.Warn("Sanitizer", $"本次清洗修正了 {anomalyCount} 个异常值");

        return result;
    }

    /// <summary>温度范围检查，超出范围时用上次好值或安全默认值替换</summary>
    private static int SanitizeTemp(ref double value, double? lastGood, string label, PluginLogger? logger)
    {
        if (value >= MinTemp && value <= MaxTemp) return 0;
        var fallback = lastGood.HasValue && lastGood.Value >= MinTemp && lastGood.Value <= MaxTemp
            ? lastGood.Value : 45.0; // 安全默认温度
        logger?.Warn("Sanitizer", $"{label}异常: {value:F1}°C 超出范围 [{MinTemp},{MaxTemp}]，回退为 {fallback:F1}°C");
        value = fallback;
        return 1;
    }

    /// <summary>负载范围检查</summary>
    private static int SanitizeLoad(ref double value, double? lastGood, string label, PluginLogger? logger)
    {
        if (value >= MinLoad && value <= MaxLoad) return 0;
        var fallback = lastGood.HasValue && lastGood.Value >= MinLoad && lastGood.Value <= MaxLoad
            ? lastGood.Value : 50.0; // 安全默认负载
        logger?.Warn("Sanitizer", $"{label}异常: {value:F1}% 超出范围 [{MinLoad},{MaxLoad}]，回退为 {fallback:F1}%");
        value = fallback;
        return 1;
    }

    /// <summary>风扇百分比范围检查</summary>
    private static int SanitizeFan(ref double value, double? lastGood, string label, PluginLogger? logger)
    {
        if (value >= MinFanPercent && value <= MaxFanPercent) return 0;
        var fallback = lastGood.HasValue && lastGood.Value >= MinFanPercent && lastGood.Value <= MaxFanPercent
            ? lastGood.Value : 50.0;
        logger?.Warn("Sanitizer", $"{label}异常: {value:F1}% 超出范围 [{MinFanPercent},{MaxFanPercent}]，回退为 {fallback:F1}%");
        value = fallback;
        return 1;
    }

    /// <summary>跳变检测：单次采样变化超过阈值则回退为上次好值</summary>
    private static int DetectSpike(ref double value, double lastGood, double threshold, string label, PluginLogger? logger)
    {
        var delta = Math.Abs(value - lastGood);
        if (delta <= threshold) return 0;
        logger?.Warn("Sanitizer", $"{label}跳变异常: {lastGood:F1} -> {value:F1}（变化 {delta:F1} 超过阈值 {threshold}），回退为 {lastGood:F1}");
        value = lastGood;
        return 1;
    }
}
