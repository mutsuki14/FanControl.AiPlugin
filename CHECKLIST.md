# 首次使用检查清单

按顺序完成以下步骤，确保插件正常工作。

## 环境准备

- [ ] Windows 10/11 系统
- [ ] 已安装 [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（用于编译）
- [ ] 已安装 [.NET 8.0 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0)（用于运行配置工具）
- [ ] 已安装 [FanControl](https://getfancontrol.com/)
- [ ] 准备好 AI 服务的 API Key 和端点信息

## 编译

- [ ] 运行 `Scripts\build-all.bat`（或手动 `dotnet build`）
- [ ] 确认编译成功，无报错

## 配置

- [ ] 运行 `Scripts\start-config-tool.bat` 启动配置工具
- [ ] 在 AI 服务标签页填写：
  - [ ] 端点 URL
  - [ ] API Key
  - [ ] 模型名称
- [ ] 点击"测试连接"确认连接成功
- [ ] 在传感器标签页将 `sensorProvider` 改为 `lhm`
- [ ] 点击"保存配置"

## 部署

- [ ] 运行 `Scripts\deploy-plugin.bat`（或手动复制文件）
- [ ] 确认 FanControl Plugins 目录包含：
  - [ ] `FanControl.AiPlugin.dll`
  - [ ] `LibreHardwareMonitorLib.dll`
  - [ ] `HidSharp.dll`
  - [ ] `ai-fan-settings.json`（已编辑）

## 验证

- [ ] 以管理员身份启动 FanControl
- [ ] 确认传感器列表出现 AI 相关条目
- [ ] 观察风扇是否根据温度自动调整
- [ ] （可选）启用诊断日志确认 AI 调用正常

## 故障排除

如果遇到问题：
1. 查看 `Docs\04-troubleshooting.md`
2. 启用诊断日志（`enableDiagnostics: true, logLevel: debug, logToFile: true`）
3. 检查 Plugins 目录下的 `ai-fan-plugin.log`

---

全部完成后，你的 FanControl 就已经接入 AI 智能风扇控制了！
