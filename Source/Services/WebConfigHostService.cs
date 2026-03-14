using System.Text.Json;
using FanControl.AiPlugin.Config;
using FanControl.AiPlugin.Logging;

namespace FanControl.AiPlugin.Services;

public sealed class WebConfigHostService
{
    private readonly PluginLogger _logger;

    public WebConfigHostService(PluginLogger logger)
    {
        _logger = logger;
    }

    public bool TryValidateJson(string json, out string? error)
    {
        try
        {
            JsonDocument.Parse(json);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warn("WebPanel", $"JSON 校验失败: {ex.Message}");
            error = ex.Message;
            return false;
        }
    }
}
