# 配置说明

本文档说明 `ai-fan-settings.json` 的主要配置项，包含基础连接、场景模式、传感器绑定、成本控制、学习模式、Webhook、本地 Web 面板等。

## 基础配置

| 字段 | 类型 | 说明 |
|------|------|------|
| `enabled` | bool | 是否启用插件 |
| `apiKey` | string | 模型服务 API Key |
| `endpointUrl` | string | OpenAI Chat Completions 兼容端点 |
| `model` | string | 模型名称 |
| `sensorProvider` | string | `mock` 或 `lhm` |
| `promptLanguage` | string | `zh-CN` 或 `en-US` |

## 场景模式

可选场景：`quiet`、`balanced`、`performance`、`gaming`

相关字段：
- `activeScenario`
- `enableScenarioSchedule`
- `scenarioSchedule`
- `scenarioProfiles`

每个场景可独立配置：
- `systemPromptOverride`
- `aiTemperature`
- `highTemperatureThresholdC`
- `minimumFanPercent`
- `fanBiasPercent`
- `temperatureOffsetC`

## 安全与回退

| 字段 | 说明 |
|------|------|
| `dangerousTemperatureC` | 达到该温度时直接全速绕过 AI |
| `highTemperatureThresholdC` | 高温时的强制抬升阈值 |
| `maxStepChangePercent` | 单次最大调节步进 |
| `minFanPercent` | 基础最低风扇转速 |
| `enableLocalFallback` | AI 失败时使用本地规则 |

## AI 调用优化

| 字段 | 说明 |
|------|------|
| `changeThreshold` | 温度/负载变化低于阈值时可跳过 AI |
| `hysteresisPercent` | 微小变化不生效，避免抖动 |
| `snapshotHistorySize` | 历史快照数量 |
| `enableAiResponseCache` | 启用 AI 响应缓存 |
| `cacheReuseTemperatureDelta` | 温度波动低于该值时复用结果 |
| `maxDailyEstimatedCostUsd` | 达到成本阈值后优先学习规则 |

## 传感器与风扇映射

| 字段 | 说明 |
|------|------|
| `cpuSensorName` | CPU 温度传感器名称 |
| `gpuSensorName` | GPU 温度传感器名称 |
| `motherboardSensorName` | 主板温度传感器名称 |
| `sensorMatchMode` | `contains` 或 `exact` |
| `cpuFanPrimarySource` | CPU 风扇主要参考源 |
| `gpuFanPrimarySource` | GPU 风扇主要参考源 |
| `caseFanPrimarySource` | 机箱风扇主要参考源 |

## 日志、诊断与仪表盘

| 字段 | 说明 |
|------|------|
| `enableDiagnostics` | 启用诊断 |
| `logLevel` | `debug/info/warning/error` |
| `logToFile` | 写入独立日志 |
| `logRotateMaxFileSizeMb` | 单文件最大体积 |
| `logRotateMaxFiles` | 保留滚动文件数量 |
| `dashboardOutputPath` | 仪表盘状态文件路径 |

## 学习模式

| 字段 | 说明 |
|------|------|
| `enableLearningMode` | 启用学习模式 |
| `learningDataPath` | 历史样本 JSONL 路径 |
| `learningMinSamples` | 生成本地规则的最小样本数 |
| `preferLearnedRulesWhenBudgetExceeded` | 成本超限时优先本地规则 |

## Webhook 与本地 Web 面板

| 字段 | 说明 |
|------|------|
| `enableWebhookNotification` | 启用 Webhook 通知 |
| `webhookUrl` | 企业微信/钉钉/Telegram 等入口 |
| `enableWebPanel` | 启动本地 Web 配置面板 |
| `webPanelPort` | 本地面板端口 |

## 示例

请参考仓库中的 `Source/ai-fan-settings.json` 与 `Plugin/ai-fan-settings.json`。
