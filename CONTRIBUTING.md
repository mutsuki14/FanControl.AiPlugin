# Contributing / 贡献指南

感谢你关注这个项目。

这个仓库目前以中文文档为主，适合继续开发、在 Windows 上编译部署，或逐步整理成公开项目。提交修改前，建议先阅读以下文档：

- `README.md`
- `CHECKLIST.md`
- `Docs/01-quick-start.md`
- `Docs/02-deployment.md`
- `Docs/03-configuration.md`
- `Docs/04-troubleshooting.md`

## 开发环境建议

推荐环境：

- Windows 10/11
- .NET 8.0 SDK
- FanControl 可用安装环境
- 如果需要真实传感器支持，请准备 LibreHardwareMonitor 可访问的硬件环境

## Build / 编译建议

在仓库根目录优先使用：

```cmd
Scripts\build-all.bat
```

或手动：

```cmd
cd Source
dotnet build -c Release -p:USE_LHM=true
```

如果你只验证基础逻辑，也可以先不启用 `USE_LHM`。

## 提交前检查

提交前请至少确认：

- 没有提交真实 API Key、令牌、密码或个人隐私信息
- 没有提交 `bin/`、`obj/`、日志文件、临时文件、IDE 配置文件
- 配置示例中的 `apiKey` 仍然是占位符
- `README.md`、`Docs/` 与实际行为没有明显不一致
- 如果改了配置字段，同步更新 `Plugin/ai-fan-settings.json`、`Source/ai-fan-settings.json` 和说明文档
- 如果改了部署方式，同步更新 `Scripts/` 和相关文档

## 安全提醒

请不要提交以下内容：

- 真实 API Key
- 本地测试用端点中的敏感地址
- 编译产物（DLL / EXE / PDB）
- 带有个人路径或机器名的日志

如果你在本地使用额外配置，建议使用 `*.local.json` 之类的文件名，并确保它不会被提交。

## 提交风格建议

建议保持提交简洁明确，例如：

- `docs: improve deployment guide`
- `feat: add sensor binding diagnostics`
- `fix: correct timeout default value in README`

## 测试建议

当前仓库还没有完整自动化测试流程，因此建议人工检查：

- 配置工具能否正常打开
- 测试连接是否可返回成功/失败信息
- 配置保存后 JSON 字段名是否正确
- 在 `mock` 与 `lhm` 两种模式下是否都能正常启动
- 诊断日志是否正常生成

## Pull Request 建议

如果后续采用 PR 协作，建议在说明中写清：

- 改动目的
- 影响范围
- 是否修改了配置字段或文档
- 在什么环境下做了验证

欢迎先从文档修正、部署说明优化、排障补充这类小改动开始。
