using FanControl.AiPlugin.Models;

namespace FanControl.AiPlugin.Sensors;

/// <summary>
/// 传感器数据提供者接口。
/// 所有硬件传感器的读取逻辑都通过此接口抽象，
/// 方便在模拟实现与真实硬件实现之间切换。
/// </summary>
public interface ISensorProvider : IDisposable
{
    /// <summary>
    /// 初始化传感器（打开硬件连接、加载驱动等）。
    /// 应在首次采集前调用一次。
    /// </summary>
    void Initialize();

    /// <summary>
    /// 采集一次完整的运行时快照。
    /// </summary>
    /// <param name="previous">上一次快照，用于计算温度趋势。首次调用时传 null。</param>
    /// <returns>包含温度、负载、风扇转速及趋势的快照对象</returns>
    FanRuntimeSnapshot Collect(FanRuntimeSnapshot? previous);
}
