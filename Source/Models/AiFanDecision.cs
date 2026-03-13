using System.Text.Json.Serialization;

namespace FanControl.AiPlugin.Models;

/// <summary>
/// AI 返回的三路风扇控制决策。
/// </summary>
public sealed class AiFanDecision
{
    /// <summary>CPU 风扇建议转速（0~100%）</summary>
    [JsonPropertyName("cpuFanPercent")]
    public double CpuFanPercent { get; set; }

    /// <summary>GPU 风扇建议转速（0~100%）</summary>
    [JsonPropertyName("gpuFanPercent")]
    public double GpuFanPercent { get; set; }

    /// <summary>机箱风扇建议转速（0~100%）</summary>
    [JsonPropertyName("caseFanPercent")]
    public double CaseFanPercent { get; set; }

    /// <summary>总体模式：quiet / balanced / performance / emergency</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "balanced";

    /// <summary>决策理由</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>过热风险标志</summary>
    [JsonPropertyName("isOverheatWarning")]
    public bool IsOverheatWarning { get; set; }

    /// <summary>来源：true=AI，false=本地回退</summary>
    [JsonIgnore]
    public bool IsFromAi { get; set; } = true;

    /// <summary>有效模式</summary>
    public static readonly string[] ValidModes = ["quiet", "balanced", "performance", "emergency"];

    /// <summary>格式化三路决策</summary>
    public void PrintTo(TextWriter w, string title)
    {
        w.WriteLine($"  {title}\uff1a");
        w.WriteLine($"     CPU  \u98ce\u6247: {CpuFanPercent,6:F1}%");
        w.WriteLine($"     GPU  \u98ce\u6247: {GpuFanPercent,6:F1}%");
        w.WriteLine($"     \u673a\u7bb1\u98ce\u6247:  {CaseFanPercent,6:F1}%");
        w.WriteLine($"     \u6a21\u5f0f:      {Mode}");
        w.WriteLine($"     \u8fc7\u70ed\u8b66\u544a:  {(IsOverheatWarning ? "\u662f" : "\u5426")}");
        w.WriteLine($"     \u6765\u6e90:      {(IsFromAi ? "AI" : "\u672c\u5730\u56de\u9000")}");
        w.WriteLine($"     \u8bf4\u660e:      {Reason}");
    }
}
