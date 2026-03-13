namespace FanControl.AiPlugin.Config;

/// <summary>
/// AI 服务提供商的连接配置。
/// 用户在 ai-fan-settings.json 中填写模型名称、API Key、聊天端点地址及调控参数。
/// </summary>
public sealed class AiProviderSettings
{
    /// <summary>模型名称，如 "gpt-4o"、"deepseek-chat"</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>API 密钥</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>完整的聊天补全端点地址</summary>
    public string EndpointUrl { get; set; } = string.Empty;

    /// <summary>请求超时（秒）</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>模型温度（0.0~2.0）</summary>
    public double Temperature { get; set; } = 0.3;

    /// <summary>单次最大步进变化百分比，防止风扇剧烈跳变</summary>
    public double MaxStepPercent { get; set; } = 15.0;

    /// <summary>轮询间隔（秒），插件模式下每隔多久请求一次 AI 决策</summary>
    public int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// 传感器提供者类型：mock 或 lhm（LibreHardwareMonitor）。
    /// 默认 mock，使用模拟数据；设为 lhm 启用真实硬件传感器。
    /// 注意：使用 lhm 需要以 USE_LHM=true 编译。
    /// </summary>
    public string SensorProvider { get; set; } = "mock";

    // ── 传感器绑定配置（用户可自定义传感器名称） ──

    /// <summary>
    /// CPU 温度传感器名称（精确匹配或模糊包含）。
    /// 留空则使用自动匹配逻辑。
    /// 示例: "CPU Package"、"Core (Tctl/Tdie)"
    /// </summary>
    public string CpuSensorName { get; set; } = string.Empty;

    /// <summary>
    /// GPU 温度传感器名称（精确匹配或模糊包含）。
    /// 留空则使用自动匹配逻辑。
    /// 示例: "GPU Core"、"GPU Hot Spot"
    /// </summary>
    public string GpuSensorName { get; set; } = string.Empty;

    /// <summary>
    /// 主板温度传感器名称（精确匹配或模糊包含）。
    /// 留空则使用自动匹配逻辑。
    /// 示例: "System"、"Temperature #2"
    /// </summary>
    public string MotherboardSensorName { get; set; } = string.Empty;

    /// <summary>
    /// 传感器名称匹配模式: "exact" 精确匹配 / "contains" 模糊包含（默认）。
    /// 模糊包含模式下，只要传感器名称包含指定字符串即视为匹配（不区分大小写）。
    /// </summary>
    public string SensorMatchMode { get; set; } = "contains";

    /// <summary>是否使用精确匹配模式</summary>
    public bool UseExactMatch => string.Equals(SensorMatchMode, "exact", StringComparison.OrdinalIgnoreCase);

    // ── 诊断与日志配置 ──

    /// <summary>是否启用诊断日志（默认关闭）</summary>
    public bool EnableDiagnostics { get; set; } = false;

    /// <summary>日志级别：debug / info / warning / error（默认 info）</summary>
    public string LogLevel { get; set; } = "info";

    /// <summary>是否将日志写入文件（默认关闭，仅控制台输出）</summary>
    public bool LogToFile { get; set; } = false;

    /// <summary>检查配置是否有效</summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Model)
            && !string.IsNullOrWhiteSpace(ApiKey)
            && !string.IsNullOrWhiteSpace(EndpointUrl)
            && Uri.TryCreate(EndpointUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>是否请求使用 LibreHardwareMonitor 真实传感器</summary>
    public bool UseLhm => string.Equals(SensorProvider, "lhm", StringComparison.OrdinalIgnoreCase);

    /// <summary>返回配置摘要（隐藏 Key）</summary>
    public override string ToString()
    {
        var masked = string.IsNullOrEmpty(ApiKey) ? "(未设置)"
            : ApiKey.Length > 8 ? ApiKey[..4] + "****" + ApiKey[^4..] : "****";
        var sensorBindings = new List<string>();
        if (!string.IsNullOrWhiteSpace(CpuSensorName)) sensorBindings.Add($"CPU={CpuSensorName}");
        if (!string.IsNullOrWhiteSpace(GpuSensorName)) sensorBindings.Add($"GPU={GpuSensorName}");
        if (!string.IsNullOrWhiteSpace(MotherboardSensorName)) sensorBindings.Add($"MB={MotherboardSensorName}");
        var bindingStr = sensorBindings.Count > 0
            ? $"[{string.Join(", ", sensorBindings)}]({SensorMatchMode})"
            : "(自动匹配)";

        return $"模型:{Model} 端点:{EndpointUrl} Key:{masked} 超时:{TimeoutSeconds}s "
             + $"步进:+/-{MaxStepPercent}% 轮询:{PollingIntervalSeconds}s 传感器:{SensorProvider} "
             + $"绑定:{bindingStr} "
             + $"诊断:{(EnableDiagnostics ? "开" : "关")} 日志级别:{LogLevel} 写文件:{(LogToFile ? "是" : "否")}";
    }
}
