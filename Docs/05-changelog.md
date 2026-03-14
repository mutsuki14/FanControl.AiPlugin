# 更新说明

## 版本历史

---

### v8.1 — DirectUse（可直接部署版）
**日期**：2026-03-14

**修复**：
- 修复 LibreHardwareMonitor 真实传感器版本构建失败问题
- 移除当前依赖版本中不存在的 `IsFanControllerEnabled` 初始化项
- 将 `SensorBindingResult` 调整为 record，修复绑定结果复制逻辑编译错误

**发布**：
- 产出可直接部署到 FanControl `Plugins` 目录的主插件 DLL
- 打包 `LibreHardwareMonitorLib.dll` 与 `HidSharp.dll`
- 打包默认配置文件 `ai-fan-settings.json`
- 打包 Windows 配置工具发布目录
- 准备 GitHub Release 压缩包 `FanControl.AiPlugin-v8.1-DirectUse.zip`

---

### v8 — FeatureExpansion（15 项功能扩展版）
**日期**：2026-03-14

**新增**：
- 场景/模式系统：`quiet`、`balanced`、`performance`、`gaming`
- 场景定时切换：`enableScenarioSchedule` + `scenarioSchedule`
- 多语言 Prompt：`promptLanguage`
- 风扇映射配置：`cpuFanPrimarySource`、`gpuFanPrimarySource`、`caseFanPrimarySource`
- 紧急安全保护：`dangerousTemperatureC` 达到即绕过 AI 直接全速
- 配置热重载：监控 `ai-fan-settings.json` 自动重载
- 本地 Web 配置面板：`enableWebPanel` + `webPanelPort`
- API 用量统计：记录 token 使用量与估算费用
- 性能监控仪表盘：输出 `ai-fan-dashboard.json`
- AI 响应缓存：`enableAiResponseCache` + `cacheReuseTemperatureDelta`
- 学习模式：记录 `ai-fan-learning.jsonl` 并尝试本地规则回放
- 曲线导出：输出 `ai-fan-graph-curves.json`
- Webhook 通知：AI 故障或高温时推送告警
- 独立日志轮转：`logRotateMaxFileSizeMb` + `logRotateMaxFiles`

**改进**：
- `OpenAiCompatibleClient` 现在返回结构化响应，含延迟、状态码、token 与估算费用
- `AiDecisionService` 现在根据场景和语言动态构建 Prompt，并带上映射、历史摘要、成本提示与场景专属 temperature
- `ScenarioProfileResolver` 支持每个场景独立 Prompt 覆盖、高温阈值、最小风扇与偏置参数
- `FanControlPluginAdapter` 接入缓存、学习模式、热统计、Webhook、安全旁路、成本阈值优先学习与增强仪表盘输出
- `WebConfigHostService` 保存配置前增加 JSON 校验，避免错误配置直接覆盖
- `AiFanPlugin` 接入热重载与本地 Web 面板生命周期
- 默认配置文件和文档已同步到新字段集合
