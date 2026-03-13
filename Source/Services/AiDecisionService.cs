using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FanControl.AiPlugin.Config;
using FanControl.AiPlugin.Logging;
using FanControl.AiPlugin.Models;

namespace FanControl.AiPlugin.Services;

/// <summary>
/// AI 决策服务：协调 AI 客户端、安全守卫和本地回退，
/// 完成"运行时数据 → AI 分析 → 安全校验 → 三路决策"的全流程。
/// 支持快照历史上下文和更严格的 JSON 输出约束。
/// </summary>
public sealed partial class AiDecisionService : IDisposable
{
    private readonly OpenAiCompatibleClient _client;
    private readonly double _maxStep;
    private readonly PluginLogger _logger;

    /// <summary>发送给 AI 的系统提示词（严格 JSON 约束）</summary>
    private const string SystemPrompt = """
        你是一个智能风扇控制助手，需要独立控制三路风扇：CPU 风扇、GPU 风扇、机箱风扇。

        ## 输出格式（严格要求）

        你的回复必须是且仅是一个合法的 JSON 对象，不得包含任何其他文字、注释、markdown 代码块标记（如 ```json）或额外说明。
        违反此格式要求将导致解析失败。

        必须严格遵循以下 JSON 结构，所有 6 个字段缺一不可：
        {"cpuFanPercent":50.0,"gpuFanPercent":45.0,"caseFanPercent":35.0,"mode":"balanced","reason":"简要理由","isOverheatWarning":false}

        字段约束：
        - cpuFanPercent: number, 范围 [20, 100]
        - gpuFanPercent: number, 范围 [20, 100]
        - caseFanPercent: number, 范围 [20, 100]
        - mode: string, 必须是 "quiet" | "balanced" | "performance" | "emergency" 之一
        - reason: string, 中文，不超过 50 字
        - isOverheatWarning: boolean

        ## 决策原则

        1. 三路风扇应独立决策：CPU 风扇主要参考 CPU 温度和负载，GPU 风扇主要参考 GPU 数据，
           机箱风扇综合考虑整体散热，可以略低于 CPU/GPU 风扇。
        2. 关注温度趋势字段：正值表示升温中，趋势越大应提前增加风扇转速。
        3. 如果提供了历史快照数据，应综合分析温度变化趋势，做出更平滑的决策。
        4. 低温低负载 → quiet 模式，转速 20~35%
        5. 中温中负载 → balanced 模式，转速 35~55%
        6. 高温高负载 → performance 模式，转速 55~80%
        7. 极高温 → emergency 模式，转速 80~100%
        8. 决策应保持连续性，避免风扇转速剧烈跳变。
        """;

    public AiDecisionService(AiProviderSettings settings, PluginLogger? logger = null)
    {
        _logger = logger ?? new PluginLogger();
        _client = new OpenAiCompatibleClient(settings, _logger);
        _maxStep = settings.MaxStepPercent;
    }

    /// <summary>测试连接</summary>
    public Task<(bool Success, string Message)> TestConnectionAsync()
        => _client.TestConnectionAsync();

    /// <summary>
    /// 获取三路风扇控制决策（带快照历史）。AI 失败时自动回退到本地策略。
    /// </summary>
    public async Task<(AiFanDecision? Raw, AiFanDecision Safe)> GetDecisionAsync(
        FanRuntimeSnapshot snapshot, List<FanRuntimeSnapshot>? history = null)
    {
        AiFanDecision? raw = null;

        try
        {
            _logger.Debug("AI", "构建用户消息并发起 AI 请求");
            var userMsg = BuildUserMessage(snapshot, history);
            var responseText = await _client.SendChatRequestAsync(userMsg, SystemPrompt);
            raw = ParseResponse(responseText);
            raw.IsFromAi = true;
            _logger.Info("AI", $"AI 响应解析成功: CPU={raw.CpuFanPercent:F1}% GPU={raw.GpuFanPercent:F1}% Case={raw.CaseFanPercent:F1}% 模式={raw.Mode}");
        }
        catch (Exception ex)
        {
            _logger.Error("AI", "AI 调用失败，回退到本地策略", ex);
        }

        var decision = raw ?? FanSafetyGuard.LocalFallback(snapshot, _logger);
        var safe = FanSafetyGuard.Enforce(decision, snapshot, _maxStep, _logger);

        return (raw, safe);
    }

    /// <summary>
    /// 同步版本——用于 IPlugin2.Update() 驱动的场景。
    /// 内部对异步调用做 GetAwaiter().GetResult() 阻塞（FanControl 的 Update 是同步调用）。
    /// </summary>
    public (AiFanDecision? Raw, AiFanDecision Safe) GetDecisionSync(
        FanRuntimeSnapshot snapshot, List<FanRuntimeSnapshot>? history = null)
    {
        return GetDecisionAsync(snapshot, history).GetAwaiter().GetResult();
    }

    /// <summary>构建包含完整运行时数据的用户消息，可包含历史快照</summary>
    private static string BuildUserMessage(FanRuntimeSnapshot s, List<FanRuntimeSnapshot>? history = null)
    {
        var sb = new StringBuilder();

        // 如果有历史快照，先输出历史摘要
        if (history is { Count: > 0 })
        {
            sb.AppendLine("最近历史快照（从旧到新）：");
            for (var i = 0; i < history.Count; i++)
            {
                var h = history[i];
                sb.AppendLine($"  [{i + 1}] {h.TimestampUtc:HH:mm:ss} CPU:{h.CpuTemperature}°C GPU:{h.GpuTemperature}°C MB:{h.MotherboardTemperature}°C CPU负载:{h.CpuUsagePercent}% GPU负载:{h.GpuUsagePercent}% 风扇:CPU={h.CurrentCpuFanPercent}%/GPU={h.CurrentGpuFanPercent}%/机箱={h.CurrentCaseFanPercent}%");
            }
            sb.AppendLine();
        }

        sb.AppendLine("当前运行时数据：");
        sb.AppendLine($"- CPU 温度: {s.CpuTemperature}°C （趋势: {s.CpuTempTrend:+0.0;-0.0} °C/min）");
        sb.AppendLine($"- GPU 温度: {s.GpuTemperature}°C （趋势: {s.GpuTempTrend:+0.0;-0.0} °C/min）");
        sb.AppendLine($"- 主板温度: {s.MotherboardTemperature}°C （趋势: {s.MotherboardTempTrend:+0.0;-0.0} °C/min）");
        sb.AppendLine($"- CPU 使用率: {s.CpuUsagePercent}%");
        sb.AppendLine($"- GPU 使用率: {s.GpuUsagePercent}%");
        sb.AppendLine($"- 当前 CPU 风扇: {s.CurrentCpuFanPercent}%");
        sb.AppendLine($"- 当前 GPU 风扇: {s.CurrentGpuFanPercent}%");
        sb.AppendLine($"- 当前机箱风扇: {s.CurrentCaseFanPercent}%");
        sb.AppendLine($"- 采集时间(UTC): {s.TimestampUtc:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("请根据以上数据给出三路风扇的独立控制建议，仅返回 JSON。");

        return sb.ToString();
    }

    /// <summary>解析 AI 响应 JSON</summary>
    private AiFanDecision ParseResponse(string text)
    {
        var json = ExtractJson(text);
        try
        {
            var d = JsonSerializer.Deserialize<AiFanDecision>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return d ?? throw new InvalidOperationException("反序列化结果为 null");
        }
        catch (JsonException ex)
        {
            _logger.Error("AI", $"AI 响应 JSON 解析失败: {ex.Message}。内容: {text[..Math.Min(text.Length, 200)]}");
            throw new InvalidOperationException(
                $"AI 响应非有效 JSON: {ex.Message}。内容: {text[..Math.Min(text.Length, 200)]}");
        }
    }

    /// <summary>从可能包含 markdown 包裹的文本中提取 JSON</summary>
    private static string ExtractJson(string text)
    {
        text = text.Trim();

        var match = JsonBlockRegex().Match(text);
        if (match.Success)
            return match.Groups[1].Value.Trim();

        var first = text.IndexOf('{');
        var last = text.LastIndexOf('}');
        if (first >= 0 && last > first)
            return text[first..(last + 1)];

        return text;
    }

    [GeneratedRegex(@"```(?:json)?\s*\n?([\s\S]*?)\n?```", RegexOptions.Compiled)]
    private static partial Regex JsonBlockRegex();

    public void Dispose() => _client.Dispose();
}
