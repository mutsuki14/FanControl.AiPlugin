using System.Text.Json;
using System.Text.Json.Serialization;

namespace FanControl.AiPlugin.Config;

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static AiProviderSettings Load(string path)
    {
        if (!File.Exists(path))
        {
            var settings = new AiProviderSettings();
            Save(path, settings);
            return settings;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AiProviderSettings>(json, JsonOptions) ?? new AiProviderSettings();
    }

    public static void Save(string path, AiProviderSettings settings)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(path, json);
    }
}
