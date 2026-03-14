using FanControl.AiPlugin.Config;
using FanControl.AiPlugin.Logging;

namespace FanControl.AiPlugin.Services;

public sealed class LearningRuleEngine
{
    private readonly AiProviderSettings _settings;
    private readonly PluginLogger _logger;

    public LearningRuleEngine(AiProviderSettings settings, PluginLogger logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public bool ShouldPreferLearnedRules(double estimatedDailyCostUsd)
    {
        return _settings.PreferLearnedRulesWhenBudgetExceeded && estimatedDailyCostUsd >= _settings.MaxDailyEstimatedCostUsd;
    }
}
