using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FanControl.AiPlugin.Config;
using FanControl.AiPlugin.Logging;

namespace FanControl.AiPlugin.Services;

/// <summary>
/// 通用 OpenAI 兼容 API 客户端。
/// 支持 OpenAI、Azure OpenAI、DeepSeek、Ollama 等。
/// </summary>
public sealed class OpenAiCompatibleClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AiProviderSettings _settings;
    private readonly PluginLogger _logger;

    public OpenAiCompatibleClient(AiProviderSettings settings, PluginLogger? logger = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? new PluginLogger();
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds)
        };

        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        }

        _logger.Debug("HTTP", $"HTTP 客户端已初始化: 端点={settings.EndpointUrl} 超时={settings.TimeoutSeconds}s");
    }

    /// <summary>测试连接</summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        _logger.Info("HTTP", "开始连接测试...");
        try
        {
            var response = await SendChatRequestAsync("\u4f60\u597d\uff0c\u8bf7\u56de\u590d\u201c\u8fde\u63a5\u6210\u529f\u201d\u56db\u4e2a\u5b57\u3002");
            _logger.Info("HTTP", $"连接测试成功: {Truncate(response, 60)}");
            return (true, $"连接成功！模型响应: {Truncate(response, 100)}");
        }
        catch (HttpRequestException ex)
        {
            _logger.Error("HTTP", "连接测试失败: HTTP 请求错误", ex);
            return (false, $"HTTP 请求失败: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            _logger.Error("HTTP", $"连接测试失败: 请求超时（{_settings.TimeoutSeconds}秒）");
            return (false, $"请求超时（{_settings.TimeoutSeconds}秒）。请检查端点地址是否正确。");
        }
        catch (Exception ex)
        {
            _logger.Error("HTTP", "连接测试失败", ex);
            return (false, $"连接测试失败: {ex.Message}");
        }
    }

    /// <summary>发送聊天请求</summary>
    public async Task<string> SendChatRequestAsync(string userMessage, string? systemPrompt = null)
    {
        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            messages.Add(new { role = "system", content = systemPrompt });
        messages.Add(new { role = "user", content = userMessage });

        var body = new
        {
            model = _settings.Model,
            messages,
            temperature = _settings.Temperature,
            max_tokens = 1024
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.Debug("HTTP", $"发送请求: 模型={_settings.Model} 消息长度={userMessage.Length}");

        var resp = await _httpClient.PostAsync(_settings.EndpointUrl, content);
        var text = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            _logger.Error("HTTP", $"API 返回错误状态码 {(int)resp.StatusCode}: {Truncate(text, 200)}");
            throw new HttpRequestException($"API 返回状态码 {(int)resp.StatusCode}: {Truncate(text, 500)}");
        }

        _logger.Debug("HTTP", $"收到响应: 状态码={resp.StatusCode} 长度={text.Length}");

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;

        if (root.TryGetProperty("choices", out var choices)
            && choices.GetArrayLength() > 0
            && choices[0].TryGetProperty("message", out var msg)
            && msg.TryGetProperty("content", out var c))
        {
            return c.GetString() ?? string.Empty;
        }

        _logger.Error("HTTP", $"无法解析 AI 响应结构: {Truncate(text, 200)}");
        throw new InvalidOperationException($"无法解析 AI 响应: {Truncate(text, 300)}");
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) ? string.Empty : s.Length <= max ? s : s[..max] + "...";

    public void Dispose() => _httpClient.Dispose();
}
