namespace FanControl.AiPlugin.ConfigTool;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // 支持命令行指定配置文件路径
        if (args.Length > 0 && File.Exists(args[0]))
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(args[0])) ?? Environment.CurrentDirectory;
        }

        Application.Run(new ConfigForm());
    }
}
