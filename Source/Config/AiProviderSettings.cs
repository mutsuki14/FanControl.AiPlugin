namespace FanControl.AiPlugin.Config;

public sealed class AiProviderSettings
{
    public bool Enabled { get; set; } = true;
    public string ApiKey { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string Model { get; set; } = "gpt-4o-mini";
    public string SensorProvider { get; set; } = "mock";
    public string PromptLanguage { get; set; } = "zh-CN";
    public string ActiveScenario { get; set; } = "balanced";
    public bool EnableScenarioSchedule { get; set; }
    public List<ScenarioScheduleEntry> ScenarioSchedule { get; set; } = [];
    public Dictionary<string, ScenarioProfile> ScenarioProfiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool EnableDiagnostics { get; set; } = true;
    public string LogLevel { get; set; } = "info";
    public bool LogToFile { get; set; } = true;
    public int LogRotateMaxFileSizeMb { get; set; } = 5;
    public int LogRotateMaxFiles { get; set; } = 5;
    public double DangerousTemperatureC { get; set; } = 95;
    public double HighTemperatureThresholdC { get; set; } = 80;
    public double MinFanPercent { get; set; } = 20;
    public double MaxStepChangePercent { get; set; } = 12;
    public double ChangeThreshold { get; set; } = 2;
    public double HysteresisPercent { get; set; } = 3;
    public int SnapshotHistorySize { get; set; } = 12;
    public bool EnableAiResponseCache { get; set; } = true;
    public double CacheReuseTemperatureDelta { get; set; } = 2;
    public bool EnableLearningMode { get; set; } = true;
    public int LearningMinSamples { get; set; } = 50;
    public string LearningDataPath { get; set; } = "ai-fan-learning.jsonl";
    public bool PreferLearnedRulesWhenBudgetExceeded { get; set; } = true;
    public double MaxDailyEstimatedCostUsd { get; set; } = 1;
    public string DashboardOutputPath { get; set; } = "ai-fan-dashboard.json";
    public string ExportGraphCurvePath { get; set; } = "ai-fan-graph-curves.json";
    public bool EnableWebhookNotification { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public bool EnableWebPanel { get; set; }
    public int WebPanelPort { get; set; } = 18765;
    public string CpuSensorName { get; set; } = string.Empty;
    public string GpuSensorName { get; set; } = string.Empty;
    public string MotherboardSensorName { get; set; } = string.Empty;
    public string SensorMatchMode { get; set; } = "contains";
    public string CpuFanPrimarySource { get; set; } = "cpu";
    public string GpuFanPrimarySource { get; set; } = "gpu";
    public string CaseFanPrimarySource { get; set; } = "hybrid";
}
