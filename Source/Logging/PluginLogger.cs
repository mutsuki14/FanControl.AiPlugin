using System.Reflection;
using System.Text;

namespace FanControl.AiPlugin.Logging;

/// <summary>
/// 插件日志服务：支持控制台输出和文件写入。
/// 线程安全，支持日志级别过滤。
///
/// 日志级别（从低到高）：
///   Debug → Info → Warning → Error
///
/// 设置 LogLevel 为 Info 时，Debug 消息不输出。
/// 设置 LogLevel 为 Error 时，只输出 Error 级别。
/// </summary>
public sealed class PluginLogger : IDisposable
{
    /// <summary>日志级别枚举</summary>
    public enum Level
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    private readonly bool _enabled;
    private readonly Level _minLevel;
    private readonly bool _logToFile;
    private readonly string _logFilePath;
    private readonly object _fileLock = new();
    private StreamWriter? _fileWriter;
    private bool _disposed;

    /// <summary>日志文件完整路径（只读）</summary>
    public string LogFilePath => _logFilePath;

    /// <summary>日志是否启用</summary>
    public bool Enabled => _enabled;

    /// <summary>
    /// 创建日志服务实例。
    /// </summary>
    /// <param name="enabled">是否启用诊断日志</param>
    /// <param name="minLevel">最低日志级别</param>
    /// <param name="logToFile">是否写入文件</param>
    public PluginLogger(bool enabled, Level minLevel, bool logToFile)
    {
        _enabled = enabled;
        _minLevel = minLevel;
        _logToFile = logToFile;

        // 日志文件路径：与插件 DLL 同目录
        var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var dir = !string.IsNullOrEmpty(asmDir) ? asmDir : Directory.GetCurrentDirectory();
        _logFilePath = Path.Combine(dir, "ai-fan-plugin.log");

        if (_enabled && _logToFile)
        {
            try
            {
                _fileWriter = new StreamWriter(_logFilePath, append: true, Encoding.UTF8)
                {
                    AutoFlush = true
                };
                WriteRaw($"========== 日志启动 {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [Logger] 无法打开日志文件: {ex.Message}");
                _fileWriter = null;
            }
        }
    }

    /// <summary>使用默认配置创建（诊断关闭）</summary>
    public PluginLogger() : this(false, Level.Info, false) { }

    /// <summary>记录 Debug 级别消息</summary>
    public void Debug(string tag, string message) => Log(Level.Debug, tag, message);

    /// <summary>记录 Info 级别消息</summary>
    public void Info(string tag, string message) => Log(Level.Info, tag, message);

    /// <summary>记录 Warning 级别消息</summary>
    public void Warn(string tag, string message) => Log(Level.Warning, tag, message);

    /// <summary>记录 Error 级别消息</summary>
    public void Error(string tag, string message) => Log(Level.Error, tag, message);

    /// <summary>记录 Error 级别消息（带异常信息）</summary>
    public void Error(string tag, string message, Exception ex)
        => Log(Level.Error, tag, $"{message}: {ex.Message}");

    /// <summary>
    /// 核心日志方法：格式化并输出日志消息。
    /// 格式: [时间] [级别] [标签] 消息
    /// </summary>
    private void Log(Level level, string tag, string message)
    {
        if (!_enabled) return;
        if (level < _minLevel) return;

        var levelStr = level switch
        {
            Level.Debug => "DEBUG",
            Level.Info => "INFO ",
            Level.Warning => "WARN ",
            Level.Error => "ERROR",
            _ => "?    "
        };

        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{levelStr}] [{tag}] {message}";

        // 始终输出到控制台
        Console.WriteLine($"  {line}");

        // 有条件写入文件
        if (_logToFile && _fileWriter is not null)
        {
            WriteToFile(line);
        }
    }

    /// <summary>线程安全地写入文件</summary>
    private void WriteToFile(string line)
    {
        lock (_fileLock)
        {
            try
            {
                _fileWriter?.WriteLine(line);
            }
            catch
            {
                // 文件写入失败时静默忽略，不影响插件运行
            }
        }
    }

    /// <summary>写入原始文本行（无格式化）</summary>
    private void WriteRaw(string text)
    {
        lock (_fileLock)
        {
            try
            {
                _fileWriter?.WriteLine(text);
            }
            catch { }
        }
    }

    /// <summary>
    /// 解析日志级别字符串。
    /// 支持: debug, info, warning/warn, error（不区分大小写）。
    /// </summary>
    public static Level ParseLevel(string? levelStr)
    {
        if (string.IsNullOrWhiteSpace(levelStr))
            return Level.Info;

        return levelStr.Trim().ToLowerInvariant() switch
        {
            "debug" => Level.Debug,
            "info" => Level.Info,
            "warning" or "warn" => Level.Warning,
            "error" => Level.Error,
            _ => Level.Info
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_fileWriter is not null)
        {
            WriteRaw($"========== 日志关闭 {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========");
            lock (_fileLock)
            {
                _fileWriter.Flush();
                _fileWriter.Dispose();
                _fileWriter = null;
            }
        }
    }
}
