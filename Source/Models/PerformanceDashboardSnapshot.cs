namespace FanControl.AiPlugin.Models;

public sealed class PerformanceDashboardSnapshot
{
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public int AiCallSuccessCount { get; set; }
    public int AiCallFailureCount { get; set; }
    public double AverageAiLatencyMs { get; set; }
    public double AverageRecommendedFanPercent { get; set; }
    public double EstimatedDailyCostUsd { get; set; }
    public double SuccessRate { get; set; }
    public int LocalFallbackCount { get; set; }
    public string LastDecisionSource { get; set; } = string.Empty;
}
