# CHANGELOG

本文件记录项目从概念验证到当前发布整理版的主要阶段演进。

## v5 - 最终发布整理版

- 整理为面向实际交付的发布目录结构
- 新增 `Plugin/`、`ConfigTool/`、`Docs/`、`Scripts/`、`Source/`
- 增加中文快速开始、部署、配置、故障排查、更新说明文档
- 增加 Windows 批处理脚本，便于编译、配置和部署
- 增加 `CHECKLIST.md` 作为首次使用检查清单

## v4 - 配置界面版

- 增加独立 WinForms 配置工具
- 支持通过界面编辑模型、API Key、端点、诊断和传感器绑定配置
- 增加测试连接能力
- 保持与主插件共用同一份 `ai-fan-settings.json`

## v3 - 传感器绑定版

- 增加 `cpuSensorName`、`gpuSensorName`、`motherboardSensorName`
- 增加 `sensorMatchMode`，支持 `contains` 与 `exact`
- 支持用户指定名称优先匹配，未命中时回退自动匹配
- 把绑定结果写入日志与诊断摘要

## v2 - 诊断增强版

- 增加插件日志系统
- 增加诊断摘要导出
- 增加 AI 调用成功、失败、本地回退和安全修正统计
- 增强对传感器绑定、AI 请求和风扇决策的排障能力

## v1 - 基础可运行版

- 基于 FanControl 插件接口实现 AI 风扇控制基础结构
- 支持 OpenAI 兼容接口
- 支持用户自定义 `model`、`apiKey`、`endpointUrl`
- 提供基础安全守卫和本地回退逻辑

## 说明

- 这里的 v1-v5 是项目阶段标记，不等同于已经发布到 GitHub Releases 的正式标签
- 如果后续公开发布，建议从当前状态开始补正式 Git tag，例如 `v5.0`
