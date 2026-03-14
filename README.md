# FanControl AI Plugin

> 一个面向 FanControl 的 AI 风扇控制插件项目，支持 OpenAI 兼容模型接入、场景模式、热重载、本地 Web 配置面板、学习模式、Webhook 通知与诊断日志。

## 当前状态

- 主插件项目已完成真实传感器版本编译
- 可直接部署 DLL、依赖库、默认配置和配置工具发布目录
- 支持用户提供模型名、API Key 和兼容端点地址
- 已实现 15 项增强能力与 v8.1 直接部署修复

## 快速开始

1. 将 `Plugin/` 内文件复制到 FanControl `Plugins` 目录
2. 编辑 `ai-fan-settings.json`
3. 填写 `apiKey`、`endpointUrl`、`model`
4. 将 `sensorProvider` 设为 `lhm`
5. 以管理员身份启动 FanControl

详细说明请查看 `Docs/02-deployment.md` 与 `Docs/03-configuration.md`。
