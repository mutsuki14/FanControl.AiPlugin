namespace FanControl.AiPlugin.Services;

public sealed class AiDecisionService
{
    public string BuildSystemPrompt(string language, string scenarioName, string? scenarioPromptOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(scenarioPromptOverride))
            return scenarioPromptOverride;

        var chinese = language.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        if (chinese)
            return $"你是 FanControl 的 AI 风扇控制助手。当前场景是 {scenarioName}。只输出 JSON，不要输出 markdown，不要解释。";

        return $"You are the AI fan control assistant for FanControl. Current scenario: {scenarioName}. Return JSON only. No markdown. No explanation.";
    }
}
