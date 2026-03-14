# 配置说明

本文档详细说明 `ai-fan-settings.json` 中新增后的主要配置项、推荐组合与输出文件。

## 配置文件位置

- 插件运行时：与 `FanControl.AiPlugin.dll` 同目录
- 配置工具：默认读取源码目录或 EXE 所在目录下的同名配置文件
- 本地 Web 面板：读取并保存同一个 `ai-fan-settings.json`

## 完整配置示例

```json
{
  "model": "gpt-4o",
  "apiKey": "YOUR_API_KEY_HERE",
  "endpointUrl": "https://api.openai.com/v1/chat/completions",
  "timeoutSeconds": 30,
  "temperature": 0.3,
  "maxStepPercent": 15.0,
  "pollingIntervalSeconds": 5,
  "sensorProvider": "lhm",
  "cpuSensorName": "",
  "gpuSensorName": "",
  "motherboardSensorName": "",
  "sensorMatchMode": "contains",
  "changeThreshold": 2.0,
  "hysteresisPercent": 3.0,
  "snapshotHistorySize": 5,
  "enableSensorSanitize": true,
  "scenario": "balanced",
  "enableScenarioSchedule": false,
  "scenarioSchedule": [
    { "start": "09:00", "end": "18:00", "scenario": "quiet", "enabled": false },
    { "start": "19:00", "end": "23:59", "scenario": "gaming", "enabled": false }
  ],
  "promptLanguage": "zh-CN",
  "timeZoneId": "Local",
  "quietSystemPromptOverride": "",
  "balancedSystemPromptOverride": "",
  "performanceSystemPromptOverride": "",
  "gamingSystemPromptOverride": "",
  "quietTemperatureOffset": 4.0,
  "balancedTemperatureOffset": 0.0,
  "performanceTemperatureOffset": -4.0,
  "gamingTemperatureOffset": -6.0,
  "quietFanBiasPercent": -8.0,
  "balancedFanBiasPercent": 0.0,
  "performanceFanBiasPercent": 8.0,
  "gamingFanBiasPercent": 12.0,
  "quietMinFanPercent": 20.0,
  "balancedMinFanPercent": 22.0,
  "performanceMinFanPercent": 28.0,
  "gamingMinFanPercent": 30.0,
  "quietHighTemperatureC": 89.0,
  "balancedHighTemperatureC": 85.0,
  "performanceHighTemperatureC": 80.0,
  "gamingHighTemperatureC": 78.0,
  "quietAiTemperature": 0.2,
  "balancedAiTemperature": 0.3,
  "performanceAiTemperature": 0.2,
  "gamingAiTemperature": 0.15,
  "enableScenarioPromptAppendix": true,
  "scenarioPromptAppendix": "",
  "includeHistoricalSummaryInPrompt": true,
  "promptHistorySummaryCount": 3,
  "includeApiUsageHintInPrompt": false,
  "apiCostWarningThreshold": 0.0,
  "preferLearningWhenCostHigh": false,
  "allowLearningFallbackWithoutAi": true,
  "learningTemperatureMatchTolerance": 6.0,
  "learningUsageMatchTolerance": 20.0,
  "cpuFanPrimarySource": "cpu",
  "gpuFanPrimarySource": "gpu",
  "caseFanPrimarySource": "average",
  "dangerousTemperatureC": 95.0,
  "highTemperatureC": 85.0,
  "enableAiResponseCache": true,
  "cacheReuseTemperatureDelta": 2.0,
  "enableLearningMode": false,
  "learningRuleMinSamples": 20,
  "learningDataFileName": "ai-fan-learning.jsonl",
  "enableGraphExport": true,
  "graphExportFileName": "ai-fan-graph-curves.json",
  "promptCostPer1KTokens": 0.0,
  "completionCostPer1KTokens": 0.0,
  "enableDashboard": true,
  "dashboardFileName": "ai-fan-dashboard.json",
  "enableWebhook": false,
  "webhookUrl": "",
  "webhookCooldownSeconds": 300,
  "enableWebPanel": true,
  "webPanelPort": 50321,
  "enableDiagnostics": false,
  "logLevel": "info",
  "logToFile": true,
  "logRotateMaxFileSizeMb": 5,
  "logRotateMaxFiles": 5
}
```

## 重点字段

### AI 连接

- `model`：模型名称
- `apiKey`：认证密钥
- `endpointUrl`：OpenAI 兼容接口地址
- `timeoutSeconds`：请求超时秒数
- `temperature`：模型随机性，风扇控制建议 0.1~0.5
- `pollingIntervalSeconds`：两次决策之间的最小间隔
- `maxStepPercent`：单次风扇调整最大步进

### 传感器与映射

- `sensorProvider`：`mock` 或 `lhm`
- `cpuSensorName` / `gpuSensorName` / `motherboardSensorName`：可选的传感器绑定名称
- `sensorMatchMode`：`contains` 或 `exact`
- `cpuFanPrimarySource`：CPU 风扇主要参考哪一路，通常填 `cpu`
- `gpuFanPrimarySource`：GPU 风扇主要参考哪一路，通常填 `gpu`
- `caseFanPrimarySource`：机箱风扇主要参考哪一路，可填 `average`、`cpu`、`gpu`、`motherboard`

### 场景与定时切换

- `scenario`：默认场景，支持 `quiet`、`balanced`、`performance`、`gaming`
- `enableScenarioSchedule`：是否启用按时间段切换场景
- `scenarioSchedule`：本地时间规则列表
- `promptLanguage`：`zh-CN` 或 `en-US`
- `timeZoneId`：时区，填 `Local` 表示系统本地时区
- `quietSystemPromptOverride` / `balancedSystemPromptOverride` / `performanceSystemPromptOverride` / `gamingSystemPromptOverride`：为每个场景单独覆盖 System Prompt
- `quietTemperatureOffset` 等：每个场景自己的温度偏移策略
- `quietFanBiasPercent` 等：每个场景自己的风扇偏置
- `quietMinFanPercent` 等：每个场景自己的最低风扇转速
- `quietHighTemperatureC` 等：每个场景自己的高温触发点
- `quietAiTemperature` 等：每个场景自己的模型 temperature
- `enableScenarioPromptAppendix`：是否为所有场景追加统一 Prompt 附加说明
- `scenarioPromptAppendix`：统一追加到 System Prompt 末尾的内容

示例：

```json
"enableScenarioSchedule": true,
"scenarioSchedule": [
  { "start": "09:00", "end": "18:00", "scenario": "quiet", "enabled": true },
  { "start": "18:00", "end": "23:59", "scenario": "gaming", "enabled": true }
]
```

### 安全与稳定

- `dangerousTemperatureC`：达到该温度时直接触发全速安全旁路
- `highTemperatureC`：高温保底阈值
- `changeThreshold`：变化过小则跳过新的 AI 调用
- `hysteresisPercent`：风扇微小变化不落地，避免抖动
- `snapshotHistorySize`：保留给模型分析的历史快照数量
- `enableSensorSanitize`：是否启用异常传感器数据清洗

### 缓存、学习与导出

- `enableAiResponseCache`：是否启用缓存
- `cacheReuseTemperatureDelta`：温度变化小于此值时可复用上次结果
- `enableLearningMode`：记录历史并尝试本地学习规则
- `learningRuleMinSamples`：形成学习规则所需的最小样本数
- `learningDataFileName`：学习记录 JSONL 文件名
- `allowLearningFallbackWithoutAi`：允许在 AI 前直接优先使用学习规则
- `learningTemperatureMatchTolerance`：学习模式匹配温度容差
- `learningUsageMatchTolerance`：学习模式匹配负载容差
- `includeHistoricalSummaryInPrompt`：是否把历史摘要写入 Prompt
- `promptHistorySummaryCount`：历史摘要采样条数
- `enableGraphExport`：是否导出 Graph 风格曲线文件
- `graphExportFileName`：曲线导出文件名

### 用量统计与仪表盘

- `promptCostPer1KTokens`：每 1K prompt token 的估算单价
- `completionCostPer1KTokens`：每 1K completion token 的估算单价
- `enableDashboard`：是否持续写仪表盘状态文件
- `dashboardFileName`：仪表盘文件名
- `includeApiUsageHintInPrompt`：是否把成本控制提示传给模型
- `apiCostWarningThreshold`：累计估算成本达到该阈值后可触发成本策略
- `preferLearningWhenCostHigh`：达到成本阈值后优先尝试学习模式本地规则

### Webhook 与本地 Web 面板

- `enableWebhook`：是否启用通知
- `webhookUrl`：Webhook 地址
- `webhookCooldownSeconds`：同类事件通知冷却时间
- `enableWebPanel`：是否启用本地 Web 配置面板
- `webPanelPort`：本地面板监听端口

### 日志

- `enableDiagnostics`：是否启用诊断日志
- `logLevel`：`debug`、`info`、`warning`、`error`
- `logToFile`：是否写入独立日志文件
- `logRotateMaxFileSizeMb`：单个日志文件大小上限
- `logRotateMaxFiles`：最多保留多少个轮转日志

## 运行期输出文件

- `ai-fan-plugin.log`：插件独立日志
- `ai-fan-dashboard.json`：性能仪表盘和 token 统计
- `ai-fan-learning.jsonl`：学习模式采样记录
- `ai-fan-graph-curves.json`：导出的 Graph 风格曲线

## 推荐组合

### 日常均衡

```json
{
  "scenario": "balanced",
  "enableAiResponseCache": true,
  "changeThreshold": 2.0,
  "hysteresisPercent": 3.0,
  "enableDashboard": true
}
```

### 办公静音 + 晚间游戏

```json
{
  "enableScenarioSchedule": true,
  "scenarioSchedule": [
    { "start": "09:00", "end": "18:00", "scenario": "quiet", "enabled": true },
    { "start": "19:00", "end": "23:59", "scenario": "gaming", "enabled": true }
  ]
}
```

### 调试与排障

```json
{
  "enableDiagnostics": true,
  "logLevel": "debug",
  "logToFile": true,
  "enableDashboard": true,
  "enableWebhook": false
}
```

## 注意事项

- 修改配置文件后，热重载会自动应用大多数运行时参数
- 传感器提供者和绑定名称变化建议重新加载插件后验证
- 本地 Web 面板当前提供的是轻量 JSON 编辑能力，保存前会做基础校验，非法 JSON 不会覆盖原配置
- 风扇曲线导出是便于后续导入的中间格式，不是官方完整导入模板
