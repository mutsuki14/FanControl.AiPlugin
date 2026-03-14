using FanControl.AiPlugin.Config;
using FanControl.AiPlugin.Logging;
using FanControl.AiPlugin.Models;
using FanControl.AiPlugin.Sensors;
using FanControl.AiPlugin.Services;

namespace FanControl.AiPlugin.Plugin;

public sealed class FanControlPluginAdapter
{
    private readonly AiProviderSettings _settings;
    private readonly PluginLogger _logger;
    private readonly DiagnosticsSummary _diagnostics = new();
    private readonly ScenarioProfileResolver _scenarioResolver;
    private readonly LearningRuleEngine _learningRuleEngine;
    private readonly WebhookNotificationService _webhookService;
    private readonly FanSafetyGuard _safetyGuard;

    public FanControlPluginAdapter(AiProviderSettings settings, PluginLogger logger)
    {
        _settings = settings;
        _logger = logger;
        _scenarioResolver = new ScenarioProfileResolver(settings);
        _learningRuleEngine = new LearningRuleEngine(settings, logger);
        _webhookService = new WebhookNotificationService(settings, logger);
        _safetyGuard = new FanSafetyGuard(settings, logger);
    }

    public PerformanceDashboardSnapshot BuildDashboardSnapshot(int success, int failure, double avgLatencyMs, double avgFan, double estimatedDailyCostUsd, int localFallbackCount, string lastDecisionSource)
    {
        var total = success + failure;
        return new PerformanceDashboardSnapshot
        {
            AiCallSuccessCount = success,
            AiCallFailureCount = failure,
            AverageAiLatencyMs = avgLatencyMs,
            AverageRecommendedFanPercent = avgFan,
            EstimatedDailyCostUsd = estimatedDailyCostUsd,
            SuccessRate = total <= 0 ? 0 : Math.Round(success * 100.0 / total, 1),
            LocalFallbackCount = localFallbackCount,
            LastDecisionSource = lastDecisionSource
        };
    }

    public DiagnosticsSummary Diagnostics => _diagnostics;
}
