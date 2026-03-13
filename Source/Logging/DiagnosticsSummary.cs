using System.Text;
using System.Text.Json;
using FanControl.AiPlugin.Config;
using FanControl.AiPlugin.Models;

namespace FanControl.AiPlugin.Logging;

/// <summary>
/// 诊断摘要导出器：汇总插件当前状态，用于排查问题。
/// 支持导出为文本或 JSON 格式。
/// </summary>
public sealed class DiagnosticsSummary
{
    /// <summary>插件版本</summary>
    public string PluginVersion { get; set; } = "1.0.0-sensorbinding";

    /// <summary>当前传感器提供者类型</summary>
    public string SensorProviderType { get; set; } = "unknown";

    /// <summary>诊断是否启用</summary>
    public bool DiagnosticsEnabled { get; set; }

    /// <summary>日志级别</summary>
    public string LogLevel { get; set; } = "info";

    /// <summary>日志文件路径</summary>
    public string LogFilePath { get; set; } = string.Empty;

    /// <summary>最后一次快照</summary>
    public FanRuntimeSnapshot? LastSnapshot { get; set; }

    /// <summary>最后一次决策</summary>
    public AiFanDecision? LastDecision { get; set; }

    /// <summary>传感器绑定状态描述</summary>
    public List<string> SensorBindingStatus { get; set; } = [];

    /// <summary>传感器绑定详细结果（用户指定名称命中/回退等）</summary>
    public List<SensorBindingResult> SensorBindingResults { get; set; } = [];

    /// <summary>AI 调用累计成功次数</summary>
    public int AiCallSuccessCount { get; set; }

    /// <summary>AI 调用累计失败次数</summary>
    public int AiCallFailureCount { get; set; }

    /// <summary>本地回退触发次数</summary>
    public int LocalFallbackCount { get; set; }

    /// <summary>安全守卫修正次数</summary>
    public int SafetyGuardCorrectionCount { get; set; }

    /// <summary>最后一次 AI 调用时间</summary>
    public DateTime? LastAiCallUtc { get; set; }

    /// <summary>插件启动时间</summary>
    public DateTime StartTimeUtc { get; set; } = DateTime.UtcNow;

    /// <summary>导出为格式化文本</summary>
    public string ExportAsText()
    {
        var sb = new StringBuilder();

        sb.AppendLine("╔═══════════════════════════════════════════════════╗");
        sb.AppendLine("║          AI 风扇控制插件 — 诊断摘要              ║");
        sb.AppendLine("╚═══════════════════════════════════════════════════╝");
        sb.AppendLine();

        sb.AppendLine("【基本信息】");
        sb.AppendLine($"  插件版本:       {PluginVersion}");
        sb.AppendLine($"  传感器类型:     {SensorProviderType}");
        sb.AppendLine($"  诊断启用:       {(DiagnosticsEnabled ? "是" : "否")}");
        sb.AppendLine($"  日志级别:       {LogLevel}");
        sb.AppendLine($"  日志文件:       {LogFilePath}");
        sb.AppendLine($"  插件启动时间:   {StartTimeUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"  运行时长:       {(DateTime.UtcNow - StartTimeUtc):hh\\:mm\\:ss}");
        sb.AppendLine();

        sb.AppendLine("【AI 调用统计】");
        sb.AppendLine($"  成功次数:       {AiCallSuccessCount}");
        sb.AppendLine($"  失败次数:       {AiCallFailureCount}");
        sb.AppendLine($"  本地回退次数:   {LocalFallbackCount}");
        sb.AppendLine($"  安全修正次数:   {SafetyGuardCorrectionCount}");
        sb.AppendLine($"  最后AI调用:     {(LastAiCallUtc.HasValue ? LastAiCallUtc.Value.ToString("yyyy-MM-dd HH:mm:ss") + " UTC" : "(未调用)")}");
        sb.AppendLine();

        sb.AppendLine("【传感器绑定状态】");
        if (SensorBindingStatus.Count == 0)
            sb.AppendLine("  (\u65e0\u7ed1\u5b9a\u4fe1\u606f)");
        else
            foreach (var s in SensorBindingStatus)
                sb.AppendLine($"  {s}");

        if (SensorBindingResults.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("\u3010\u4f20\u611f\u5668\u7ed1\u5b9a\u8be6\u60c5\uff08\u7528\u6237\u6307\u5b9a\u540d\u79f0\u5339\u914d\uff09\u3011");
            foreach (var r in SensorBindingResults)
                sb.AppendLine($"  {r}");
        }
        sb.AppendLine();

        sb.AppendLine("【最新快照】");
        if (LastSnapshot is not null)
            sb.AppendLine($"  {LastSnapshot}");
        else
            sb.AppendLine("  (无快照)");
        sb.AppendLine();

        sb.AppendLine("【最新决策】");
        if (LastDecision is not null)
        {
            sb.AppendLine($"  CPU 风扇: {LastDecision.CpuFanPercent:F1}%");
            sb.AppendLine($"  GPU 风扇: {LastDecision.GpuFanPercent:F1}%");
            sb.AppendLine($"  机箱风扇: {LastDecision.CaseFanPercent:F1}%");
            sb.AppendLine($"  模式:     {LastDecision.Mode}");
            sb.AppendLine($"  来源:     {(LastDecision.IsFromAi ? "AI" : "本地回退")}");
            sb.AppendLine($"  说明:     {LastDecision.Reason}");
        }
        else
        {
            sb.AppendLine("  (无决策)");
        }

        return sb.ToString();
    }

    /// <summary>导出为 JSON 字符串</summary>
    public string ExportAsJson()
    {
        var data = new
        {
            pluginVersion = PluginVersion,
            sensorProviderType = SensorProviderType,
            diagnosticsEnabled = DiagnosticsEnabled,
            logLevel = LogLevel,
            logFilePath = LogFilePath,
            startTimeUtc = StartTimeUtc,
            uptimeSeconds = (DateTime.UtcNow - StartTimeUtc).TotalSeconds,
            aiCallSuccessCount = AiCallSuccessCount,
            aiCallFailureCount = AiCallFailureCount,
            localFallbackCount = LocalFallbackCount,
            safetyGuardCorrectionCount = SafetyGuardCorrectionCount,
            lastAiCallUtc = LastAiCallUtc,
            sensorBindingStatus = SensorBindingStatus,
            sensorBindingResults = SensorBindingResults.Select(r => new
            {
                role = r.Role,
                userSpecifiedName = r.UserSpecifiedName,
                boundSensorName = r.BoundSensorName,
                matchedByUserName = r.MatchedByUserName,
                fellBackToAuto = r.FellBackToAuto,
                isBound = r.IsBound
            }).ToList(),
            lastSnapshot = LastSnapshot is null ? null : new
            {
                cpuTemp = LastSnapshot.CpuTemperature,
                gpuTemp = LastSnapshot.GpuTemperature,
                mbTemp = LastSnapshot.MotherboardTemperature,
                cpuLoad = LastSnapshot.CpuUsagePercent,
                gpuLoad = LastSnapshot.GpuUsagePercent,
                cpuFan = LastSnapshot.CurrentCpuFanPercent,
                gpuFan = LastSnapshot.CurrentGpuFanPercent,
                caseFan = LastSnapshot.CurrentCaseFanPercent,
                timestamp = LastSnapshot.TimestampUtc
            },
            lastDecision = LastDecision is null ? null : new
            {
                cpuFanPercent = LastDecision.CpuFanPercent,
                gpuFanPercent = LastDecision.GpuFanPercent,
                caseFanPercent = LastDecision.CaseFanPercent,
                mode = LastDecision.Mode,
                reason = LastDecision.Reason,
                isFromAi = LastDecision.IsFromAi,
                isOverheatWarning = LastDecision.IsOverheatWarning
            }
        };

        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>将诊断摘要保存到文件</summary>
    public void SaveToFile(string? path = null)
    {
        path ??= Path.Combine(
            Path.GetDirectoryName(LogFilePath) ?? Directory.GetCurrentDirectory(),
            "ai-fan-diagnostics.txt");

        try
        {
            File.WriteAllText(path, ExportAsText());
        }
        catch
        {
            // 静默忽略文件写入失败
        }
    }
}
