using FanControl.Plugins;

namespace FanControl.AiPlugin.Plugin;

/// <summary>
/// AI 温度传感器——实现真实 IPluginSensor 接口。
/// 向 FanControl 暴露 AI 感知到的温度值（CPU / GPU / 主板）。
/// FanControl 每个轮询周期调用 Update()，插件在此更新 Value。
/// </summary>
public sealed class AiTempSensor : IPluginSensor
{
    private readonly Func<float?> _valueGetter;

    /// <summary>传感器唯一 ID（FanControl 内部使用）</summary>
    public string Id { get; }

    /// <summary>传感器名称（显示在 FanControl UI 上）</summary>
    public string Name { get; }

    /// <summary>当前温度值（°C），null 表示暂无数据</summary>
    public float? Value { get; private set; }

    /// <param name="name">显示名称，如 "AI-CPU-Temp"</param>
    /// <param name="id">唯一标识，如 "ai_cpu_temp"</param>
    /// <param name="valueGetter">每次 Update 时调用的取值委托</param>
    public AiTempSensor(string name, string id, Func<float?> valueGetter)
    {
        Name = name;
        Id = id;
        _valueGetter = valueGetter;
    }

    /// <summary>
    /// FanControl 每个轮询周期调用。
    /// 从适配器缓存中读取最新温度值。
    /// </summary>
    public void Update()
    {
        Value = _valueGetter();
    }
}
