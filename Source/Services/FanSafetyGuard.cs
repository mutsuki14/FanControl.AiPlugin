using FanControl.AiPlugin.Logging;
using FanControl.AiPlugin.Models;

namespace FanControl.AiPlugin.Services;

/// <summary>
/// 风扇安全守卫：对三路风扇独立执行限幅、紧急保护、步进限制与模式校验。
/// 所有决策（AI 或本地回退）都必须经过此模块才能应用到硬件。
/// </summary>
public static class FanSafetyGuard
{
    private const double MinPercent = 20.0;   // 最低不停转
    private const double MaxPercent = 100.0;
    private const double EmergencyTemp = 95.0;
    private const double CpuHighTemp = 80.0;
    private const double GpuHighTemp = 85.0;
    private const double MbHighTemp = 55.0;
    private const double TrendBoostThreshold = 5.0; // °C/min
    private const double TrendBoostMinFan = 60.0;

    /// <summary>
    /// 对三路决策执行全部安全规则。
    /// </summary>
    public static AiFanDecision Enforce(AiFanDecision input, FanRuntimeSnapshot snap, double maxStep, PluginLogger? logger = null)
    {
        var result = new AiFanDecision
        {
            CpuFanPercent    = input.CpuFanPercent,
            GpuFanPercent    = input.GpuFanPercent,
            CaseFanPercent   = input.CaseFanPercent,
            Mode             = input.Mode,
            Reason           = input.Reason,
            IsOverheatWarning = input.IsOverheatWarning,
            IsFromAi         = input.IsFromAi
        };

        var origCpu = result.CpuFanPercent;
        var origGpu = result.GpuFanPercent;
        var origCase = result.CaseFanPercent;

        // ── 1. 三路独立限幅 ──
        result.CpuFanPercent  = Clamp(result.CpuFanPercent);
        result.GpuFanPercent  = Clamp(result.GpuFanPercent);
        result.CaseFanPercent = Clamp(result.CaseFanPercent);

        if (result.CpuFanPercent != origCpu || result.GpuFanPercent != origGpu || result.CaseFanPercent != origCase)
            logger?.Debug("Safety", $"限幅修正: CPU {origCpu:F1}->{result.CpuFanPercent:F1} GPU {origGpu:F1}->{result.GpuFanPercent:F1} Case {origCase:F1}->{result.CaseFanPercent:F1}");

        // ── 2. 温度紧急保护 ──
        if (snap.CpuTemperature >= EmergencyTemp
            || snap.GpuTemperature >= EmergencyTemp
            || snap.MotherboardTemperature >= EmergencyTemp)
        {
            result.CpuFanPercent = result.GpuFanPercent = result.CaseFanPercent = 100;
            result.Mode = "emergency";
            result.IsOverheatWarning = true;
            result.Reason = $"紧急保护: 检测到 >={EmergencyTemp}°C 的极端温度";
            logger?.Warn("Safety", $"紧急保护触发! CPU={snap.CpuTemperature}°C GPU={snap.GpuTemperature}°C MB={snap.MotherboardTemperature}°C -> 全速运转");
            return result; // 紧急模式跳过步进限制
        }

        // ── 3. 高温保底 ──
        if (snap.CpuTemperature >= CpuHighTemp)
        {
            var before = result.CpuFanPercent;
            result.CpuFanPercent = Math.Max(result.CpuFanPercent, 70);
            if (result.CpuFanPercent != before)
                logger?.Debug("Safety", $"CPU 高温保底: {before:F1}% -> {result.CpuFanPercent:F1}% (CPU={snap.CpuTemperature}°C >= {CpuHighTemp}°C)");
        }
        if (snap.GpuTemperature >= GpuHighTemp)
        {
            var before = result.GpuFanPercent;
            result.GpuFanPercent = Math.Max(result.GpuFanPercent, 70);
            if (result.GpuFanPercent != before)
                logger?.Debug("Safety", $"GPU 高温保底: {before:F1}% -> {result.GpuFanPercent:F1}% (GPU={snap.GpuTemperature}°C >= {GpuHighTemp}°C)");
        }
        if (snap.MotherboardTemperature >= MbHighTemp)
        {
            var before = result.CaseFanPercent;
            result.CaseFanPercent = Math.Max(result.CaseFanPercent, 70);
            if (result.CaseFanPercent != before)
                logger?.Debug("Safety", $"主板高温保底: {before:F1}% -> {result.CaseFanPercent:F1}% (MB={snap.MotherboardTemperature}°C >= {MbHighTemp}°C)");
        }

        // ── 4. 温度趋势预判 ──
        if (snap.CpuTempTrend >= TrendBoostThreshold)
        {
            var before = result.CpuFanPercent;
            result.CpuFanPercent = Math.Max(result.CpuFanPercent, TrendBoostMinFan);
            if (result.CpuFanPercent != before)
                logger?.Debug("Safety", $"CPU 趋势预判: 升温 {snap.CpuTempTrend:+0.0}°C/min -> 风扇 {before:F1}% -> {result.CpuFanPercent:F1}%");
        }
        if (snap.GpuTempTrend >= TrendBoostThreshold)
        {
            var before = result.GpuFanPercent;
            result.GpuFanPercent = Math.Max(result.GpuFanPercent, TrendBoostMinFan);
            if (result.GpuFanPercent != before)
                logger?.Debug("Safety", $"GPU 趋势预判: 升温 {snap.GpuTempTrend:+0.0}°C/min -> 风扇 {before:F1}% -> {result.GpuFanPercent:F1}%");
        }
        if (snap.MotherboardTempTrend >= TrendBoostThreshold)
        {
            var before = result.CaseFanPercent;
            result.CaseFanPercent = Math.Max(result.CaseFanPercent, TrendBoostMinFan);
            if (result.CaseFanPercent != before)
                logger?.Debug("Safety", $"主板趋势预判: 升温 {snap.MotherboardTempTrend:+0.0}°C/min -> 风扇 {before:F1}% -> {result.CaseFanPercent:F1}%");
        }

        // ── 5. 步进限制（±maxStep） ──
        var preCpu = result.CpuFanPercent;
        var preGpu = result.GpuFanPercent;
        var preCase = result.CaseFanPercent;

        result.CpuFanPercent  = Step(result.CpuFanPercent,  snap.CurrentCpuFanPercent,  maxStep);
        result.GpuFanPercent  = Step(result.GpuFanPercent,  snap.CurrentGpuFanPercent,  maxStep);
        result.CaseFanPercent = Step(result.CaseFanPercent, snap.CurrentCaseFanPercent, maxStep);

        if (result.CpuFanPercent != preCpu || result.GpuFanPercent != preGpu || result.CaseFanPercent != preCase)
            logger?.Debug("Safety", $"步进限制(+/-{maxStep}%): CPU {preCpu:F1}->{result.CpuFanPercent:F1} GPU {preGpu:F1}->{result.GpuFanPercent:F1} Case {preCase:F1}->{result.CaseFanPercent:F1}");

        // ── 6. 模式校验 ──
        if (!AiFanDecision.ValidModes.Contains(result.Mode))
        {
            logger?.Warn("Safety", $"无效模式 \"{result.Mode}\"，修正为 balanced");
            result.Mode = "balanced";
        }

        // 高温禁止 quiet
        if (result.Mode == "quiet"
            && (snap.CpuTemperature >= CpuHighTemp || snap.GpuTemperature >= GpuHighTemp))
        {
            logger?.Warn("Safety", $"高温下禁止 quiet 模式，修正为 balanced (CPU={snap.CpuTemperature}°C GPU={snap.GpuTemperature}°C)");
            result.Mode = "balanced";
        }

        return result;
    }

    /// <summary>本地回退策略：根据温度分段线性映射到转速</summary>
    public static AiFanDecision LocalFallback(FanRuntimeSnapshot snap, PluginLogger? logger = null)
    {
        logger?.Info("Safety", "触发本地回退策略（温度分段映射）");

        var cpuFan  = Curve(snap.CpuTemperature);
        var gpuFan  = Curve(snap.GpuTemperature);
        var caseFan = Curve(Math.Max(snap.MotherboardTemperature, Math.Max(snap.CpuTemperature, snap.GpuTemperature) - 10));

        var maxTemp = Math.Max(snap.CpuTemperature, Math.Max(snap.GpuTemperature, snap.MotherboardTemperature));
        var mode = maxTemp switch
        {
            >= 90 => "emergency",
            >= 75 => "performance",
            >= 55 => "balanced",
            _     => "quiet"
        };

        logger?.Debug("Safety", $"本地回退结果: CPU={cpuFan:F1}% GPU={gpuFan:F1}% Case={caseFan:F1}% 模式={mode}");

        return new AiFanDecision
        {
            CpuFanPercent    = cpuFan,
            GpuFanPercent    = gpuFan,
            CaseFanPercent   = caseFan,
            Mode             = mode,
            Reason           = "本地回退策略（温度分段映射）",
            IsOverheatWarning = maxTemp >= 90,
            IsFromAi         = false
        };
    }

    /// <summary>限幅 [20, 100]</summary>
    private static double Clamp(double v) => Math.Clamp(v, MinPercent, MaxPercent);

    /// <summary>步进限制</summary>
    private static double Step(double target, double current, double maxStep)
    {
        if (maxStep <= 0) return target;
        var diff = target - current;
        if (Math.Abs(diff) <= maxStep) return target;
        return current + Math.Sign(diff) * maxStep;
    }

    /// <summary>分段线性温度→转速曲线</summary>
    private static double Curve(double temp) => temp switch
    {
        <= 40 => 25,
        <= 55 => 25 + (temp - 40) / 15 * 20,      // 25~45%
        <= 70 => 45 + (temp - 55) / 15 * 25,       // 45~70%
        <= 85 => 70 + (temp - 70) / 15 * 20,       // 70~90%
        _     => 100
    };
}
