using FanControl.AiPlugin.Config;

namespace FanControl.AiPlugin.Services;

public sealed class ScenarioProfileResolver
{
    private readonly AiProviderSettings _settings;

    public ScenarioProfileResolver(AiProviderSettings settings)
    {
        _settings = settings;
    }

    public string ResolveActiveScenario(DateTime localTime)
    {
        if (_settings.EnableScenarioSchedule)
        {
            var current = localTime.TimeOfDay;
            foreach (var entry in _settings.ScenarioSchedule)
            {
                if (TimeSpan.TryParse(entry.StartLocalTime, out var start) && TimeSpan.TryParse(entry.EndLocalTime, out var end))
                {
                    var inRange = start <= end ? current >= start && current < end : current >= start || current < end;
                    if (inRange && !string.IsNullOrWhiteSpace(entry.ScenarioName))
                        return entry.ScenarioName;
                }
            }
        }

        return string.IsNullOrWhiteSpace(_settings.ActiveScenario) ? "balanced" : _settings.ActiveScenario;
    }

    public ScenarioProfile GetProfile(string scenarioName)
    {
        if (_settings.ScenarioProfiles.TryGetValue(scenarioName, out var profile))
            return profile;

        return scenarioName.ToLowerInvariant() switch
        {
            "quiet" => new ScenarioProfile { AiTemperature = 0.2, MinimumFanPercent = 18, HighTemperatureThresholdC = 78, FanBiasPercent = -5 },
            "performance" => new ScenarioProfile { AiTemperature = 0.35, MinimumFanPercent = 28, HighTemperatureThresholdC = 75, FanBiasPercent = 8 },
            "gaming" => new ScenarioProfile { AiTemperature = 0.4, MinimumFanPercent = 32, HighTemperatureThresholdC = 74, FanBiasPercent = 12 },
            _ => new ScenarioProfile { AiTemperature = 0.3, MinimumFanPercent = 20, HighTemperatureThresholdC = 80, FanBiasPercent = 0 }
        };
    }
}
