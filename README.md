# FanControl AI Plugin — Release Pack

用 AI 控制 PC 风扇转速的 FanControl 插件发布包。

包含完整源码、可视化配置工具、部署脚本和中文文档，开箱即用。

## 发布包结构

```
FanControlAiPluginReleasePack/
│
├── README.md                  ← 本文件（发布总说明）
├── CHECKLIST.md               ← 首次使用检查清单
│
├── Plugin/                    ← 插件部署目录
│   ├── ai-fan-settings.json   ← 默认配置文件（需编辑）
│   └── README_PLUGIN.md       ← 插件部署说明
│
├── ConfigTool/                ← 配置工具目录
│   └── README_CONFIGTOOL.md   ← 配置工具说明
│
├── Docs/                      ← 中文文档
│   ├── 01-quick-start.md      ← 快速开始
│   ├── 02-deployment.md       ← 部署步骤
│   ├── 03-configuration.md    ← 配置说明（15 字段详解）
│   ├── 04-troubleshooting.md  ← 故障排查
│   └── 05-changelog.md        ← 更新说明（v1~v5）
│
├── Scripts/                   ← Windows 批处理脚本
│   ├── build-all.bat          ← 一键编译（插件+配置工具+Demo）
│   ├── deploy-plugin.bat      ← 一键部署到 FanControl
│   ├── start-config-tool.bat  ← 启动配置工具
│   └── run-demo.bat           ← 运行 Demo 控制台
│
└── Source/                    ← 完整源码
    ├── FanControlAiPluginConfigUI.sln
    ├── FanControl.AiPlugin.csproj
    ├── ai-fan-settings.json
    ├── Config/                ← 配置模型与读写
    ├── Models/                ← 数据模型
    ├── Services/              ← AI 决策/安全守卫/HTTP 客户端
    ├── Sensors/               ← 传感器抽象与实现
    ├── Plugin/                ← FanControl 插件接口实现
    ├── Logging/               ← 日志与诊断
    ├── ConfigTool/            ← WinForms 配置工具源码
    └── Demo/                  ← 演示控制台
```

## 快速开始

### 1. 编译

```cmd
Scripts\build-all.bat
```

或手动：

```cmd
cd Source
dotnet build -c Release -p:USE_LHM=true
```

### 2. 配置

```cmd
Scripts\start-config-tool.bat
```

在配置工具中：
- 填写 AI 端点 URL、API Key、模型名称
- 点击"测试连接"验证
- 将 `sensorProvider` 改为 `lhm`
- 点击"保存配置"

### 3. 部署

```cmd
Scripts\deploy-plugin.bat
```

或手动将编译产物和 `ai-fan-settings.json` 复制到 FanControl 的 `Plugins` 目录。

### 4. 运行

以管理员身份启动 FanControl，插件自动加载。

## 核心功能

| 功能 | 说明 |
|------|------|
| AI 风扇控制 | 通过 OpenAI 兼容接口智能调节 CPU/GPU/机箱风扇 |
| 可视化配置 | WinForms 配置工具，支持全部 15 个配置字段编辑 |
| 测试连接 | 一键验证 AI 端点/Key/模型可用性 |
| 真实传感器 | LibreHardwareMonitor 读取硬件温度 |
| 传感器绑定 | 自定义传感器名称，支持模糊/精确匹配 |
| 多层安全 | 限幅、紧急、高温保底、趋势预判、步进限制 |
| 诊断日志 | 4 级日志，控制台+文件输出 |

## 支持的 AI 服务

| 服务 | 端点 |
|------|------|
| OpenAI | `https://api.openai.com/v1/chat/completions` |
| Azure OpenAI | `https://{resource}.openai.azure.com/...` |
| DeepSeek | `https://api.deepseek.com/v1/chat/completions` |
| Ollama (本地) | `http://localhost:11434/v1/chat/completions` |

任何兼容 OpenAI Chat Completions API 的服务均可使用。

## 安全机制

插件内置 5 层安全保护，确保风扇不会因 AI 误判而导致过热：

1. **限幅**：风扇 20%~100%，绝不停转
2. **紧急**：>= 95°C 全速运转
3. **高温保底**：CPU >= 80°C / GPU >= 85°C / 主板 >= 55°C 不低于 70%
4. **趋势预判**：升温 >= 5°C/min 提前增速
5. **步进限制**：每次 +/- 15%，防突变

## 文档索引

| 文档 | 说明 |
|------|------|
| [CHECKLIST.md](CHECKLIST.md) | 首次使用检查清单 |
| [Docs/01-quick-start.md](Docs/01-quick-start.md) | 快速开始（5 分钟上手） |
| [Docs/02-deployment.md](Docs/02-deployment.md) | 详细部署步骤 |
| [Docs/03-configuration.md](Docs/03-configuration.md) | 全部配置字段详解 |
| [Docs/04-troubleshooting.md](Docs/04-troubleshooting.md) | 故障排查 |
| [Docs/05-changelog.md](Docs/05-changelog.md) | 版本更新说明 |
| [Plugin/README_PLUGIN.md](Plugin/README_PLUGIN.md) | 插件部署文件说明 |
| [ConfigTool/README_CONFIGTOOL.md](ConfigTool/README_CONFIGTOOL.md) | 配置工具说明 |

## 技术信息

- **运行时**：.NET 8.0（插件/Demo）/ .NET 8.0 Windows（配置工具）
- **插件接口**：FanControl.Plugins (IPlugin2, IPluginSensor, IPluginControlSensor)
- **传感器库**：LibreHardwareMonitorLib 0.9.4（条件编译 USE_LHM）
- **AI 协议**：OpenAI Chat Completions 兼容接口
- **配置格式**：JSON（camelCase 字段名）
- **编程语言**：C# 12

## 已知限制

1. 配置工具为独立 WinForms 程序，不嵌入 FanControl 界面
2. 修改配置后需重启 FanControl 生效
3. 传感器绑定仅在 LHM 模式下生效
4. 三路传感器共用同一匹配模式
5. 当前环境无法编译 Windows DLL（需在 Windows + .NET 8.0 SDK 环境下编译）
