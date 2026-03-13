using FanControl.Plugins;

namespace FanControl.AiPlugin.Plugin;

/// <summary>
/// AI 风扇控制传感器——实现真实 IPluginControlSensor 接口。
/// 向 FanControl 暴露 AI 决策的三路风扇转速（CPU / GPU / 机箱）。
///
/// 工作模式：
/// - AI 自动模式：Value 由适配器后台更新，FanControl 读取此值
/// - FanControl 覆盖模式：FanControl 调用 Set() 写入外部指定值
/// - Reset() 恢复为 AI 自动模式
/// </summary>
public sealed class AiControlSensor : IPluginControlSensor
{
    private readonly Func<float?> _aiValueGetter;
    private float? _overrideValue;
    private bool _isOverridden;

    /// <summary>控制通道唯一 ID</summary>
    public string Id { get; }

    /// <summary>控制通道名称（显示在 FanControl UI 上）</summary>
    public string Name { get; }

    /// <summary>
    /// 当前风扇转速百分比。
    /// 覆盖模式下返回 FanControl 设定的值，否则返回 AI 决策值。
    /// </summary>
    public float? Value { get; private set; }

    /// <param name="name">显示名称，如 "AI-CPU-Fan"</param>
    /// <param name="id">唯一标识，如 "ai_cpu_fan"</param>
    /// <param name="aiValueGetter">AI 决策值的取值委托</param>
    public AiControlSensor(string name, string id, Func<float?> aiValueGetter)
    {
        Name = name;
        Id = id;
        _aiValueGetter = aiValueGetter;
    }

    /// <summary>
    /// FanControl 每个轮询周期调用。
    /// 覆盖模式下使用覆盖值，否则从 AI 决策获取。
    /// </summary>
    public void Update()
    {
        Value = _isOverridden ? _overrideValue : _aiValueGetter();
    }

    /// <summary>
    /// FanControl 调用：设置目标转速百分比（覆盖 AI 决策）。
    /// </summary>
    /// <param name="val">目标转速百分比（0~100）</param>
    public void Set(float val)
    {
        _overrideValue = val;
        _isOverridden = true;
    }

    /// <summary>
    /// FanControl 调用：释放控制权，恢复为 AI 自动决策。
    /// </summary>
    public void Reset()
    {
        _isOverridden = false;
        _overrideValue = null;
    }
}
