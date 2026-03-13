using System.Reflection;
using System.Text.Json;
using FanControl.AiPlugin.Logging;

namespace FanControl.AiPlugin.Config;

/// <summary>
/// 配置文件读写工具。
/// 默认查找与程序集同目录下的 ai-fan-settings.json。
/// </summary>
public static class SettingsStore
{
    private const string FileName = "ai-fan-settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>获取配置文件的完整路径</summary>
    public static string GetFilePath()
    {
        // 优先使用程序集所在目录（插件 DLL 旁边）
        var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrEmpty(asmDir))
        {
            var path = Path.Combine(asmDir, FileName);
            if (File.Exists(path)) return path;
        }

        // 回退到当前工作目录
        var cwdPath = Path.Combine(Directory.GetCurrentDirectory(), FileName);
        if (File.Exists(cwdPath)) return cwdPath;

        // 默认返回程序集目录下的路径（即使不存在）
        return !string.IsNullOrEmpty(asmDir)
            ? Path.Combine(asmDir, FileName)
            : Path.Combine(Directory.GetCurrentDirectory(), FileName);
    }

    /// <summary>加载配置，文件不存在时返回默认值</summary>
    public static AiProviderSettings Load(PluginLogger? logger = null)
    {
        var path = GetFilePath();

        if (!File.Exists(path))
        {
            Console.WriteLine($"  配置文件不存在: {path}");
            Console.WriteLine("  使用默认配置（请编辑 ai-fan-settings.json 后重试）");
            logger?.Warn("Config", $"配置文件不存在: {path}，使用默认配置");
            var defaults = new AiProviderSettings();
            Save(defaults, path);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AiProviderSettings>(json, JsonOptions);
            Console.WriteLine($"  配置已加载: {path}");
            logger?.Info("Config", $"配置已加载: {path}");
            return settings ?? new AiProviderSettings();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  配置加载失败: {ex.Message}");
            logger?.Error("Config", "配置加载失败", ex);
            return new AiProviderSettings();
        }
    }

    /// <summary>保存配置到文件</summary>
    public static void Save(AiProviderSettings settings, string? path = null)
    {
        path ??= GetFilePath();
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(path, json);
    }
}
