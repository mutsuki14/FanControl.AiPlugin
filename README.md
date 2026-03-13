# FanControl AI Plugin

> 一个面向 FanControl 的 AI 风扇控制插件项目，支持 OpenAI 兼容模型接入、传感器绑定、可视化配置与诊断日志。
>
> An AI-powered FanControl plugin project with OpenAI-compatible model support, sensor binding, a desktop config tool, and diagnostics.

这个仓库包含完整源码、独立配置工具、部署脚本和中文文档，适合继续开发、在 Windows 上编译部署，也已经具备继续整理为公开项目的基础。

## 快速入口

- 想快速上手：看 [Docs/01-quick-start.md](Docs/01-quick-start.md)
- 想直接部署：看 [Docs/02-deployment.md](Docs/02-deployment.md)
- 想修改配置：看 [Docs/03-configuration.md](Docs/03-configuration.md)
- 想参与维护：看 [CONTRIBUTING.md](CONTRIBUTING.md)

## 安全提示

- 请不要把真实 API Key 写入准备提交到仓库的配置文件中
- 建议仅在本地环境填写真实密钥，并在提交前再次检查 `ai-fan-settings.json`
- 默认示例配置只应保留占位符，不应保留个人密钥或私有端点

## 当前状态

- 当前是首个公开版本后的持续整理阶段
- 已包含插件源码、配置工具源码、默认配置、部署脚本和故障排查文档
- 仍需要在 Windows + .NET 8.0 SDK 环境中编译生成真实 DLL 和 EXE 后再部署

## 最快上手

```cmd
Scripts\build-all.bat
Scripts\start-config-tool.bat
Scripts\deploy-plugin.bat
```

推荐顺序：先编译，再打开配置工具填写模型/API 信息并测试连接，最后部署到 FanControl 的 `Plugins` 目录。

## 仓库结构

| 目录 | 用途 |
|------|------|
| [Plugin](Plugin/README_PLUGIN.md) | 放发布用默认配置和插件部署说明 |
| [ConfigTool](ConfigTool/README_CONFIGTOOL.md) | 放配置工具使用说明 |
| [Docs](Docs/01-quick-start.md) | 中文文档入口，含快速开始、部署、配置、排障、更新说明 |
| [Scripts](Scripts/) | Windows 批处理脚本，包含编译、部署、启动配置工具、运行 Demo |
| [Source](Source/) | 完整源码，包含主插件、配置工具、Demo 与相关配置模型 |
| [CHECKLIST.md](CHECKLIST.md) | 首次部署前的检查清单 |

## 核心功能

| 功能 | 说明 |
|------|------|
| AI 风扇控制 | 通过 OpenAI 兼容接口智能调节 CPU/GPU/机箱风扇 |
| 可视化配置 | WinForms 配置工具，支持全部 15 个配置字段编辑 |
| 测试连接 | 一键验证 AI 端点、API Key、模型是否可用 |
| 真实传感器 | 基于 LibreHardwareMonitor 读取硬件温度 |
| 传感器绑定 | 自定义传感器名称，支持模糊/精确匹配 |
| 多层安全 | 限幅、紧急、高温保底、趋势预判、步进限制 |
| 诊断日志 | 4 级日志，支持控制台与文件输出 |

## 支持的 AI 服务

| 服务 | 端点 |
|------|------|
| OpenAI | `https://api.openai.com/v1/chat/completions` |
| Azure OpenAI | `https://{resource}.openai.azure.com/...` |
| DeepSeek | `https://api.deepseek.com/v1/chat/completions` |
| Ollama（本地） | `http://localhost:11434/v1/chat/completions` |

任何兼容 OpenAI Chat Completions API 的服务都可以接入。

## 文档入口

| 文档 | 说明 |
|------|------|
| [CHECKLIST.md](CHECKLIST.md) | 首次使用检查清单 |
| [CHANGELOG.md](CHANGELOG.md) | 根目录更新日志 |
| [CONTRIBUTING.md](CONTRIBUTING.md) | 贡献指南 |
| [Docs/01-quick-start.md](Docs/01-quick-start.md) | 快速开始 |
| [Docs/02-deployment.md](Docs/02-deployment.md) | 详细部署步骤 |
| [Docs/03-configuration.md](Docs/03-configuration.md) | 全部配置字段详解 |
| [Docs/04-troubleshooting.md](Docs/04-troubleshooting.md) | 故障排查 |
| [Docs/05-changelog.md](Docs/05-changelog.md) | 版本更新说明 |
| [Plugin/README_PLUGIN.md](Plugin/README_PLUGIN.md) | 插件部署说明 |
| [ConfigTool/README_CONFIGTOOL.md](ConfigTool/README_CONFIGTOOL.md) | 配置工具说明 |

## 技术信息

- 运行时：.NET 8.0（插件 / Demo）与 .NET 8.0 Windows（配置工具）
- 插件接口：FanControl.Plugins（IPlugin2、IPluginSensor、IPluginControlSensor）
- 传感器库：LibreHardwareMonitorLib 0.9.4（条件编译 `USE_LHM`）
- AI 协议：OpenAI Chat Completions 兼容接口
- 配置格式：JSON（camelCase 字段名）
- 主要语言：C# 12

## 已知限制

1. 配置工具是独立 WinForms 程序，不嵌入 FanControl 界面
2. 修改配置后需要重启 FanControl 才会生效
3. 传感器绑定仅在 `lhm` 模式下生效
4. 三路传感器共用同一匹配模式
5. 需要在 Windows + .NET 8.0 SDK 环境中编译后再实际部署
6. 当前还没有完整自动化测试或 CI 流程，主要依赖人工验证

## 许可证

本项目使用 [MIT License](LICENSE)。
