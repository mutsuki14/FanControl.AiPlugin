namespace FanControl.AiPlugin.Models;

/// <summary>
/// 运行时传感器快照：三路温度、两路负载、三路当前风扇、三路温度趋势。
/// </summary>
public sealed class FanRuntimeSnapshot
{
    // ── 温度（°C） ──
    public double CpuTemperature { get; set; }
    public double GpuTemperature { get; set; }
    public double MotherboardTemperature { get; set; }

    // ── 负载（0~100%） ──
    public double CpuUsagePercent { get; set; }
    public double GpuUsagePercent { get; set; }

    // ── 当前三路风扇转速百分比 ──
    public double CurrentCpuFanPercent { get; set; }
    public double CurrentGpuFanPercent { get; set; }
    public double CurrentCaseFanPercent { get; set; }

    // ── 温度趋势（°C/min，正=升温，负=降温） ──
    public double CpuTempTrend { get; set; }
    public double GpuTempTrend { get; set; }
    public double MotherboardTempTrend { get; set; }

    /// <summary>采集时间（UTC）</summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"CPU:{CpuTemperature}\u00b0C({CpuTempTrend:+0.0;-0.0}\u00b0C/min) {CpuUsagePercent}%\u8d1f\u8f7d \u98ce\u6247{CurrentCpuFanPercent}% | "
             + $"GPU:{GpuTemperature}\u00b0C({GpuTempTrend:+0.0;-0.0}\u00b0C/min) {GpuUsagePercent}%\u8d1f\u8f7d \u98ce\u6247{CurrentGpuFanPercent}% | "
             + $"\u4e3b\u677f:{MotherboardTemperature}\u00b0C({MotherboardTempTrend:+0.0;-0.0}\u00b0C/min) \u673a\u7bb1\u98ce\u6247{CurrentCaseFanPercent}%";
    }
}
