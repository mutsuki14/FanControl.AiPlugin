using FanControl.AiPlugin.Config;
using FanControl.AiPlugin.Logging;
using FanControl.AiPlugin.Models;
using FanControl.AiPlugin.Sensors;
using FanControl.AiPlugin.Services;
using FanControl.AiPlugin.Plugin;

// ============================================================
// FanControl.AiPlugin — Demo 控制台（诊断增强版）
// 独立于插件类库的演示程序，用于在没有 FanControl 宿主的情况下测试。
// 演示：日志初始化 → 传感器选择 → 本地回退 → AI 单次决策
//       → 模拟 Update 驱动 → 诊断摘要导出
// ============================================================

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("\u256c\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u256c");
Console.WriteLine("\u2551  AI \u98ce\u6247\u63a7\u5236 - FanControl \u771f\u5b9e\u63a5\u53e3\u5bf9\u9f50 Demo            \u2551");
Console.WriteLine("\u2551  \uff08\u8bca\u65ad\u589e\u5f3a\u7248 + LibreHardwareMonitor \u652f\u6301\uff09        \u2551");
Console.WriteLine("\u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d");
Console.WriteLine();

// ── 1. 加载配置 ──
Console.WriteLine("\u30101/8\u3011\u52a0\u8f7d AI \u670d\u52a1\u914d\u7f6e...");
var settings = SettingsStore.Load();
Console.WriteLine($"  {settings}");
Console.WriteLine();

// ── 2. 初始化日志系统 ──
Console.WriteLine("\u30102/8\u3011\u521d\u59cb\u5316\u65e5\u5fd7\u7cfb\u7edf...");
var logLevel = PluginLogger.ParseLevel(settings.LogLevel);
using var logger = new PluginLogger(settings.EnableDiagnostics, logLevel, settings.LogToFile);
Console.WriteLine($"  \u8bca\u65ad\u6a21\u5f0f: {(settings.EnableDiagnostics ? "\u5df2\u542f\u7528" : "\u5df2\u7981\u7528")}");
Console.WriteLine($"  \u65e5\u5fd7\u7ea7\u522b: {logLevel}");
Console.WriteLine($"  \u5199\u5165\u6587\u4ef6: {(settings.LogToFile ? "\u662f" : "\u5426")}");
logger.Info("Demo", "\u65e5\u5fd7\u7cfb\u7edf\u5df2\u521d\u59cb\u5316");
Console.WriteLine();

// ── 3. 选择并初始化传感器提供者 ──
Console.WriteLine("\u30103/8\u3011\u521d\u59cb\u5316\u4f20\u611f\u5668\u63d0\u4f9b\u8005...");
Console.WriteLine($"  \u914d\u7f6e\u7684\u4f20\u611f\u5668\u7c7b\u578b: {settings.SensorProvider}");
logger.Info("Demo", $"\u4f20\u611f\u5668\u63d0\u4f9b\u8005\u914d\u7f6e: {settings.SensorProvider}");

// 创建传感器绑定配置
var bindingConfig = SensorBindingConfig.FromSettings(settings);
if (bindingConfig.HasAnyBinding)
{
    Console.WriteLine($"  \u4f20\u611f\u5668\u7ed1\u5b9a: CPU=\"{bindingConfig.CpuSensorName}\" GPU=\"{bindingConfig.GpuSensorName}\" MB=\"{bindingConfig.MotherboardSensorName}\"");
    Console.WriteLine($"  \u5339\u914d\u6a21\u5f0f: {(bindingConfig.UseExactMatch ? "\u7cbe\u786e\u5339\u914d" : "\u6a21\u7cca\u5305\u542b")}");
}
else
{
    Console.WriteLine("  \u4f20\u611f\u5668\u7ed1\u5b9a: (\u672a\u914d\u7f6e\uff0c\u4f7f\u7528\u81ea\u52a8\u5339\u914d)");
}

ISensorProvider sensor;

#if USE_LIBRE_HARDWARE_MONITOR
if (settings.UseLhm)
{
    Console.WriteLine("  \u2192 \u4f7f\u7528 LibreHardwareMonitor \u771f\u5b9e\u4f20\u611f\u5668");
    Console.WriteLine("  \uff08\u63d0\u793a\uff1a\u9700\u8981\u7ba1\u7406\u5458\u6743\u9650\u624d\u80fd\u8bfb\u53d6\u90e8\u5206\u786c\u4ef6\u4f20\u611f\u5668\uff09");
    logger.Info("Demo", "\u4f7f\u7528 LibreHardwareMonitor \u771f\u5b9e\u4f20\u611f\u5668");
    sensor = new LibreHardwareMonitorSensorProvider(bindingConfig, logger);
}
else
{
    Console.WriteLine("  \u2192 \u4f7f\u7528\u6a21\u62df\u4f20\u611f\u5668\uff08\u914d\u7f6e\u4e3a mock\uff09");
    logger.Info("Demo", "\u4f7f\u7528\u6a21\u62df\u4f20\u611f\u5668");
    sensor = new MockSensorProvider(bindingConfig, logger);
}
#else
if (settings.UseLhm)
{
    Console.WriteLine("  \u26a0 \u914d\u7f6e\u8981\u6c42 LHM\uff0c\u4f46\u7f16\u8bd1\u65f6\u672a\u542f\u7528 USE_LHM");
    Console.WriteLine("  \u2192 \u56de\u9000\u5230\u6a21\u62df\u4f20\u611f\u5668");
    Console.WriteLine("  \u2192 \u542f\u7528\u65b9\u6cd5: dotnet run -p:USE_LHM=true");
    logger.Warn("Demo", "\u914d\u7f6e\u8981\u6c42 LHM \u4f46\u7f16\u8bd1\u672a\u542f\u7528\uff0c\u56de\u9000\u5230 Mock");
}
else
{
    Console.WriteLine("  \u2192 \u4f7f\u7528\u6a21\u62df\u4f20\u611f\u5668");
    logger.Info("Demo", "\u4f7f\u7528\u6a21\u62df\u4f20\u611f\u5668");
}
sensor = new MockSensorProvider(bindingConfig, logger);
#endif

sensor.Initialize();
Console.WriteLine();

// ── 4. 采集快照 ──
Console.WriteLine("\u30104/8\u3011\u91c7\u96c6\u8fd0\u884c\u65f6\u6570\u636e...");
var snapshot1 = sensor.Collect(null);
Console.WriteLine($"  \u5feb\u71671: {snapshot1}");
logger.Debug("Demo", $"\u5feb\u71671: {snapshot1}");

Console.WriteLine("  \u7b49\u5f85 1 \u79d2\u540e\u91c7\u96c6\u7b2c\u4e8c\u6b21\uff08\u8ba1\u7b97\u6e29\u5ea6\u8d8b\u52bf\uff09...");
await Task.Delay(1000);

var snapshot2 = sensor.Collect(snapshot1);
Console.WriteLine($"  \u5feb\u71672: {snapshot2}");
logger.Debug("Demo", $"\u5feb\u71672: {snapshot2}");
Console.WriteLine();

// ── 5. 本地回退演示 ──
Console.WriteLine("\u30105/8\u3011\u672c\u5730\u56de\u9000\u7b56\u7565\u6f14\u793a...");
Console.WriteLine("\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550");
var localRaw = FanSafetyGuard.LocalFallback(snapshot2, logger);
localRaw.PrintTo(Console.Out, "\u672c\u5730\u56de\u9000\u7b56\u7565");
Console.WriteLine();

var localSafe = FanSafetyGuard.Enforce(localRaw, snapshot2, settings.MaxStepPercent, logger);
localSafe.PrintTo(Console.Out, "\u5b89\u5168\u6821\u9a8c\u540e");
Console.WriteLine("\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550");
Console.WriteLine();

// ── 6. AI 单次决策 ──
if (settings.IsValid())
{
    Console.WriteLine("\u30106/8\u3011\u6d4b\u8bd5 AI \u670d\u52a1\u8fde\u63a5...");
    logger.Info("Demo", "\u5f00\u59cb AI \u670d\u52a1\u8fde\u63a5\u6d4b\u8bd5");

    ISensorProvider aiTestSensor;
#if USE_LIBRE_HARDWARE_MONITOR
    if (settings.UseLhm)
        aiTestSensor = new LibreHardwareMonitorSensorProvider(bindingConfig, logger);
    else
        aiTestSensor = new MockSensorProvider(bindingConfig, logger);
#else
    aiTestSensor = new MockSensorProvider(bindingConfig, logger);
#endif

    using var adapter = new FanControlPluginAdapter(settings, aiTestSensor, logger);

    var (ok, msg) = await adapter.TestConnectionAsync();
    Console.WriteLine(ok ? $"  [OK] {msg}" : $"  [FAIL] {msg}");
    logger.Info("Demo", $"\u8fde\u63a5\u6d4b\u8bd5: {(ok ? "OK" : "FAIL")} - {msg}");
    Console.WriteLine();

    if (ok)
    {
        Console.WriteLine("  \u8bf7\u6c42 AI \u4e09\u8def\u98ce\u6247\u63a7\u5236\u5efa\u8bae...");
        await adapter.ForceTickAsync();

        Console.WriteLine("\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550");
        var decision = adapter.LastDecision;
        if (decision is not null)
            decision.PrintTo(Console.Out, "AI \u51b3\u7b56\uff08\u5b89\u5168\u6821\u9a8c\u540e\uff09");
        Console.WriteLine("\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550");
        Console.WriteLine();
    }
}
else
{
    Console.WriteLine("\u30106/8\u3011AI \u914d\u7f6e\u65e0\u6548\uff0c\u8df3\u8fc7 AI \u8c03\u7528");
    Console.WriteLine($"  \u8bf7\u7f16\u8f91 {SettingsStore.GetFilePath()} \u586b\u5165\u6709\u6548\u914d\u7f6e");
    logger.Warn("Demo", "AI \u914d\u7f6e\u65e0\u6548\uff0c\u8df3\u8fc7 AI \u8c03\u7528");
    Console.WriteLine();
}

// ── 7. 模拟 IPlugin2.Update() 驱动 ──
Console.WriteLine("\u30107/8\u3011\u6a21\u62df FanControl \u7684 Update() \u9a71\u52a8\u6a21\u5f0f\uff085 \u8f6e\uff09...");
Console.WriteLine($"  AI \u8bf7\u6c42\u8282\u6d41\u95f4\u9694: {settings.PollingIntervalSeconds} \u79d2");
logger.Info("Demo", $"\u5f00\u59cb\u6a21\u62df Update \u9a71\u52a8\uff0c5 \u8f6e\uff0c\u95f4\u9694={settings.PollingIntervalSeconds}s");
Console.WriteLine();

ISensorProvider demoSensor;
#if USE_LIBRE_HARDWARE_MONITOR
if (settings.UseLhm)
    demoSensor = new LibreHardwareMonitorSensorProvider(bindingConfig, logger);
else
    demoSensor = new MockSensorProvider(bindingConfig, logger);
#else
demoSensor = new MockSensorProvider(bindingConfig, logger);
#endif
demoSensor.Initialize();

using var demoAdapter = new FanControlPluginAdapter(settings, demoSensor, logger);

for (var i = 1; i <= 5; i++)
{
    Console.WriteLine($"  \u2500\u2500 Update #{i} \u2500\u2500");

    demoAdapter.TickOnce();

    var snap = demoAdapter.LastSnapshot;
    var dec = demoAdapter.LastDecision;

    if (snap is not null)
        Console.WriteLine($"  \u4f20\u611f\u5668: {snap}");

    if (dec is not null)
        dec.PrintTo(Console.Out, $"  \u51b3\u7b56 #{i}");
    else
        Console.WriteLine("  \uff08\u672c\u5468\u671f\u672a\u89e6\u53d1 AI \u8c03\u7528\uff0c\u7b49\u5f85\u8282\u6d41\u95f4\u9694\u5230\u671f\uff09");

    Console.WriteLine();
    await Task.Delay(1500);
}

// ── 8. 诊断摘要导出 ──
Console.WriteLine("\u30108/8\u3011\u5bfc\u51fa\u8bca\u65ad\u6458\u8981...");
Console.WriteLine("\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550");
var diag = demoAdapter.Diagnostics;
if (diag is not null)
{
    var text = diag.ExportAsText();
    Console.WriteLine(text);

    diag.SaveToFile();
    Console.WriteLine($"  \u8bca\u65ad\u6458\u8981\u5df2\u4fdd\u5b58\u5230\u6587\u4ef6");
    logger.Info("Demo", "\u8bca\u65ad\u6458\u8981\u5df2\u5bfc\u51fa");
}
else
{
    Console.WriteLine("  \uff08\u8bca\u65ad\u6a21\u5f0f\u672a\u542f\u7528\uff09");
}
Console.WriteLine("\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550");
Console.WriteLine();

// 清理资源
sensor.Dispose();
demoSensor.Dispose();

Console.WriteLine("[OK] Demo \u5b8c\u6210\u3002");
Console.WriteLine("  * \u7c7b\u5e93\u8f93\u51fa: FanControl.AiPlugin.dll");
Console.WriteLine("  * \u653e\u5165 FanControl \u7684 Plugins \u76ee\u5f55\u5373\u53ef\u52a0\u8f7d");
Console.WriteLine("  * \u914d\u7f6e\u6587\u4ef6 ai-fan-settings.json \u653e\u5728 DLL \u540c\u76ee\u5f55");
Console.WriteLine("  * \u542f\u7528\u771f\u5b9e\u4f20\u611f\u5668: dotnet build -p:USE_LHM=true");
Console.WriteLine("  * \u542f\u7528\u8bca\u65ad\u65e5\u5fd7: \u914d\u7f6e enableDiagnostics=true, logToFile=true");
Console.WriteLine("  * \u8be6\u7ec6\u8bf4\u660e\u8bf7\u53c2\u8003 README.md");
