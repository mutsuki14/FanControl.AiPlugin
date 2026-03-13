# 快速开始

本文档帮助你在 5 分钟内完成 FanControl AI 插件的部署和首次运行。

## 前置条件

- Windows 10/11
- [FanControl](https://getfancontrol.com/) 已安装并可正常运行
- [.NET 8.0 运行时](https://dotnet.microsoft.com/download/dotnet/8.0) 已安装
- 一个可用的 OpenAI 兼容 AI 服务（OpenAI/DeepSeek/Ollama 等）

## 第一步：编译插件

```cmd
cd Source
dotnet build -c Release -p:USE_LHM=true
```

> 如果没有安装 .NET SDK，可以找已编译好的 DLL 文件。

## 第二步：部署插件

将以下文件复制到 FanControl 的 Plugins 目录（通常位于 `C:\Program Files\FanControl\Plugins\`）：

```
从 Source\bin\Release\net8.0\ 复制：
  - FanControl.AiPlugin.dll
  - LibreHardwareMonitorLib.dll（如果编译时启用了 USE_LHM）
  - HidSharp.dll（LibreHardwareMonitorLib 的依赖）

从 Plugin\ 复制：
  - ai-fan-settings.json
```

## 第三步：配置 AI 服务

### 方式 A：使用配置工具（推荐）

1. 编译配置工具：
   ```cmd
   cd Source\ConfigTool
   dotnet build -c Release
   ```
2. 运行 `Source\ConfigTool\bin\Release\net8.0-windows\FanControl.AiPlugin.ConfigTool.exe`
3. 在 AI 服务标签页填写：
   - 端点 URL
   - API Key
   - 模型名称
4. 点击"测试连接"确认可用
5. 点击"保存配置"
6. 将保存的 `ai-fan-settings.json` 复制到 FanControl Plugins 目录

### 方式 B：手动编辑 JSON

编辑 `Plugin\ai-fan-settings.json`，至少填写以下三个字段：

```json
{
  "model": "gpt-4o",
  "apiKey": "sk-your-key-here",
  "endpointUrl": "https://api.openai.com/v1/chat/completions"
}
```

## 第四步：启动 FanControl

1. 以管理员身份运行 FanControl
2. 插件会自动被加载
3. 在 FanControl 的传感器和控制列表中应能看到 AI 相关的条目

## 第五步：验证

- 检查 FanControl 是否识别到 AI 插件的传感器
- 如果启用了诊断日志（`enableDiagnostics: true`），查看 Plugins 目录下的 `ai-fan-plugin.log`

## 常见启动问题

| 问题 | 解决方案 |
|------|----------|
| 插件未显示 | 检查 DLL 是否在 Plugins 目录，重启 FanControl |
| 传感器值为 0 | 确认 `sensorProvider` 设为 `"lhm"` 并以管理员运行 |
| AI 调用失败 | 用配置工具的"测试连接"排查端点/Key/模型问题 |
| 缺少运行时 | 安装 .NET 8.0 桌面运行时 |

## 下一步

- 配置传感器名称绑定：见 [配置说明](03-configuration.md)
- 了解部署细节：见 [部署步骤](02-deployment.md)
- 遇到问题：见 [故障排查](04-troubleshooting.md)
