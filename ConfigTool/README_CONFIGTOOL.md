# ConfigTool 目录 — 配置工具发布文件

本目录用于存放 FanControl AI 插件的可视化配置工具。

## 编译后应放置的文件

```
ConfigTool/
├── FanControl.AiPlugin.ConfigTool.exe    ← 编译产物（必需）
├── FanControl.AiPlugin.dll               ← 编译产物（主库依赖）
├── FanControl.AiPlugin.ConfigTool.dll    ← 编译产物
├── FanControl.AiPlugin.ConfigTool.deps.json
├── FanControl.AiPlugin.ConfigTool.runtimeconfig.json
├── ai-fan-settings.json                  ← 配置文件（与插件共享）
└── README_CONFIGTOOL.md                  ← 本说明
```

## 编译命令

```bash
cd Source/ConfigTool
dotnet build -c Release
# 或从解决方案根目录
cd Source
dotnet build -c Release
```

编译产物位于：`Source/ConfigTool/bin/Release/net8.0-windows/`

## 启动方式

### 方式一：直接双击

双击 `FanControl.AiPlugin.ConfigTool.exe` 启动。配置工具会在当前目录查找 `ai-fan-settings.json`。

### 方式二：命令行指定配置路径

```cmd
FanControl.AiPlugin.ConfigTool.exe "C:\Program Files\FanControl\Plugins\ai-fan-settings.json"
```

### 方式三：使用启动脚本

运行发布包根目录的 `Scripts\start-config-tool.bat`，按提示操作。

## 与配置文件的关系

- 配置工具读写的文件就是插件使用的 `ai-fan-settings.json`
- 推荐将配置工具放在 FanControl Plugins 目录旁边，或指定 Plugins 目录中的 JSON 路径
- 修改配置后需重启 FanControl 才能使插件加载新配置

## 界面说明

配置工具提供三个标签页：

| 标签页 | 包含字段 |
|--------|----------|
| AI 服务 | 端点 URL、API Key、模型、温度、超时、轮询间隔、最大步进、测试连接 |
| 传感器 | 传感器提供者、CPU/GPU/主板传感器名、匹配模式 |
| 诊断 | 启用诊断、日志级别、写入日志文件 |

底部按钮：保存配置 / 重新加载 / 打开配置文件

## 测试连接

在 AI 服务标签页填写端点/Key/模型后，点击"测试连接"按钮。工具会发送一条测试消息到 AI 端点：
- **成功**：显示模型返回内容
- **失败**：显示具体错误信息（超时/401/网络错误等）

## 运行要求

- Windows 10/11
- .NET 8.0 Windows 桌面运行时 (`dotnet-runtime-8.0` + `windowsdesktop-runtime-8.0`)
- 无需管理员权限（仅编辑 JSON 文件）
