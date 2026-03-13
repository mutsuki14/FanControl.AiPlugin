using System.Text.Json;
using System.Text.RegularExpressions;
using FanControl.AiPlugin.Config;
using FanControl.AiPlugin.Logging;
using FanControl.AiPlugin.Models;

namespace FanControl.AiPlugin.Services;

/// <summary>
/// AI 决策服务：协调 AI 客户端、安全守卫和本地回退，
/// 完成"运行时数据 → AI 分析 → 安全校验 → 三路决策"的全流程。
/// </summary>
public sealed partial class AiDecisionService : IDisposable
{
    private readonly OpenAiCompatibleClient _client;
    private readonly double _maxStep;
    private readonly PluginLogger _logger;

    /// <summary>发送给 AI 的系统提示词</summary>
    private const string SystemPrompt = """
        你是一个智能风扇控制助手，需要独立控制三路风扇：CPU 风扇、GPU 风扇、机箱风扇。

        你必须以严格的 JSON 格式回复，不要包含任何其他文字或 markdown 标记。
        JSON 格式如下：
        {
          "cpuFanPercent": 50.0,
          "gpuFanPercent": 45.0,
          "caseFanPercent": 35.0,
          "mode": "balanced",
          "reason": "决策理由",
          "isOverheatWarning": false
        }

        字段说明：
        - cpuFanPercent: CPU 风扇建议转速（0~100）
        - gpuFanPercent: GPU 风扇建议转速（0~100）
        - caseFanPercent: 机箱风扇建议转速（0~100）
        - mode: 总体模式，只能是 "quiet"/"balanced"/"performance"/"emergency"
        - reason: 中文决策理由，简要说明
        - isOverheatWarning: 是否存在过热风险

        决策原则：
        1. 三路风扇应独立决策：CPU 风扇主要参考 CPU 温度和负载，GPU 风扇主要参考 GPU 数据，
           机箱风扇综合考虑整体散热，可以略低于 CPU/GPU 风扇。
        2. 关注温度趋势字段：正值表示升温中，趋势越大应提前增加风扇转速。
        3. 低温低负载 → quiet 模式，转速 20~35%
        4. 中温中负载 → balanced 模式，转速 35~55%
        5. 高温高负载 → performance 模式，转速 55~80%
        6. 极高温 → emergency 模式，转速 80~100%
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
    /// 获取三路风扇控制决策。AI 失败时自动回退到本地策略。
    /// </summary>
    public async Task<(AiFanDecision? Raw, AiFanDecision Safe)> GetDecisionAsync(FanRuntimeSnapshot snapshot)
    {
        AiFanDecision? raw = null;

        try
        {
            _logger.Debug("AI", "构建用户消息并发起 AI 请求");
            var userMsg = BuildUserMessage(snapshot);
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
    public (AiFanDecision? Raw, AiFanDecision Safe) GetDecisionSync(FanRuntimeSnapshot snapshot)
    {
        return GetDecisionAsync(snapshot).GetAwaiter().GetResult();
    }

    /// <summary>构建包含完整运行时数据的用户消息</summary>
    private static string BuildUserMessage(FanRuntimeSnapshot s)
    {
        return $"""
            当前运行时数据：
            - CPU 温度: {s.CpuTemperature}°C （趋势: {s.CpuTempTrend:+0.0;-0.0} °C/min）
            - GPU 温度: {s.GpuTemperature}°C （趋势: {s.GpuTempTrend:+0.0;-0.0} °C/min）
            - 主板温度: {s.MotherboardTemperature}°C （趋势: {s.MotherboardTempTrend:+0.0;-0.0} °C/min）
            - CPU 使用率: {s.CpuUsagePercent}%
            - GPU 使用率: {s.GpuUsagePercent}%
            - 当前 CPU 风扇: {s.CurrentCpuFanPercent}%
            - 当前 GPU 风扇: {s.CurrentGpuFanPercent}%
            - 当前机箱风扇: {s.CurrentCaseFanPercent}%
            - 采集时间(UTC): {s.TimestampUtc:yyyy-MM-dd HH:mm:ss}

            请根据以上数据给出三路风扇的独立控制建议，仅返回 JSON。
            """;
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
