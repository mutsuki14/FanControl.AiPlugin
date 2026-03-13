namespace FanControl.AiPlugin.Config;

/// <summary>
/// 用户自定义传感器绑定配置。
/// 当指定了传感器名称时，优先按名称匹配；未指定时回退到自动匹配。
/// </summary>
public sealed class SensorBindingConfig
{
    /// <summary>CPU 温度传感器名称（空=自动匹配）</summary>
    public string CpuSensorName { get; init; } = string.Empty;

    /// <summary>GPU 温度传感器名称（空=自动匹配）</summary>
    public string GpuSensorName { get; init; } = string.Empty;

    /// <summary>主板温度传感器名称（空=自动匹配）</summary>
    public string MotherboardSensorName { get; init; } = string.Empty;

    /// <summary>是否使用精确匹配（默认模糊包含）</summary>
    public bool UseExactMatch { get; init; } = false;

    /// <summary>是否有任何用户指定的传感器绑定</summary>
    public bool HasAnyBinding =>
        !string.IsNullOrWhiteSpace(CpuSensorName)
        || !string.IsNullOrWhiteSpace(GpuSensorName)
        || !string.IsNullOrWhiteSpace(MotherboardSensorName);

    /// <summary>从 AiProviderSettings 提取绑定配置</summary>
    public static SensorBindingConfig FromSettings(AiProviderSettings settings) => new()
    {
        CpuSensorName = settings.CpuSensorName?.Trim() ?? string.Empty,
        GpuSensorName = settings.GpuSensorName?.Trim() ?? string.Empty,
        MotherboardSensorName = settings.MotherboardSensorName?.Trim() ?? string.Empty,
        UseExactMatch = settings.UseExactMatch
    };
}
