namespace FanControl.AiPlugin.Config;

/// <summary>
/// 单个传感器的绑定结果，用于诊断日志。
/// </summary>
public sealed class SensorBindingResult
{
    /// <summary>传感器角色（如 "CPU 温度"、"GPU 温度"）</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>用户指定的名称（空=未指定）</summary>
    public string UserSpecifiedName { get; init; } = string.Empty;

    /// <summary>最终绑定到的传感器名称（空=未找到）</summary>
    public string BoundSensorName { get; init; } = string.Empty;

    /// <summary>是否通过用户指定名称匹配成功</summary>
    public bool MatchedByUserName { get; init; }

    /// <summary>是否回退到自动匹配</summary>
    public bool FellBackToAuto { get; init; }

    /// <summary>是否成功绑定</summary>
    public bool IsBound => !string.IsNullOrEmpty(BoundSensorName);

    public override string ToString()
    {
        if (!IsBound)
        {
            return string.IsNullOrWhiteSpace(UserSpecifiedName)
                ? $"{Role}: (未找到, 自动匹配)"
                : $"{Role}: (未找到, 用户指定=\"{UserSpecifiedName}\", 已回退自动匹配但仍未找到)";
        }

        if (MatchedByUserName)
            return $"{Role}: {BoundSensorName} (用户指定命中)";

        if (FellBackToAuto)
            return $"{Role}: {BoundSensorName} (用户指定=\"{UserSpecifiedName}\"未命中, 回退自动匹配)";

        return $"{Role}: {BoundSensorName} (自动匹配)";
    }
}
