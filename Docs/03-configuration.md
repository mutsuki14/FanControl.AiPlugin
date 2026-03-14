# 配置说明

本文档详细说明 `ai-fan-settings.json` 中每个配置项的含义、取值和使用建议。

## 配置文件位置

- **插件运行时**：与 `FanControl.AiPlugin.dll` 同目录（通常在 FanControl 的 Plugins 目录）
- **配置工具**：配置工具 EXE 所在目录，或通过命令行参数指定路径

## 完整配置示例

```json
{
  "model": "gpt-4o",
  "apiKey": "sk-your-key-here",
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
  "enableDiagnostics": false,
  "logLevel": "info",
  "logToFile": false
}
```

## 字段详解

### AI 服务配置

#### `model`（模型名称）
- **类型**：string
- **默认值**：`"gpt-4o"`
- **说明**：要调用的 AI 模型名称
- **示例**：
  - OpenAI: `"gpt-4o"`, `"gpt-4o-mini"`
  - DeepSeek: `"deepseek-chat"`
  - Ollama: `"llama3.1"`, `"qwen2.5"`

#### `apiKey`（API 密钥）
- **类型**：string
- **默认值**：空
- **说明**：AI 服务的认证密钥。Ollama 本地部署可填任意值
- **安全提示**：不要将包含真实 Key 的配置文件提交到版本控制

#### `endpointUrl`（API 端点）
- **类型**：string
- **默认值**：`"https://api.openai.com/v1/chat/completions"`
- **说明**：OpenAI Chat Completions 兼容接口地址
- **常用端点**：

| 服务 | 端点 URL |
|------|----------|
| OpenAI | `https://api.openai.com/v1/chat/completions` |
| Azure OpenAI | `https://{resource}.openai.azure.com/openai/deployments/{model}/chat/completions?api-version=2024-02-01` |
| DeepSeek | `https://api.deepseek.com/v1/chat/completions` |
| Ollama | `http://localhost:11434/v1/chat/completions` |

#### `timeoutSeconds`（请求超时）
- **类型**：int
- **默认值**：`30`
- **范围**：5 ~ 120
- **说明**：单次 AI API 请求的超时时间（秒）。网络较慢或使用大模型时建议增大

#### `temperature`（生成温度）
- **类型**：double
- **默认值**：`0.3`
- **范围**：0.0 ~ 2.0
- **说明**：控制 AI 输出的随机性。风扇控制场景建议保持低值（0.1~0.5），确保决策稳定

#### `pollingIntervalSeconds`（轮询间隔）
- **类型**：int
- **默认值**：`5`
- **范围**：1 ~ 60
- **说明**：两次 AI 调用之间的最小间隔（秒）。过小会增加 API 调用量和费用

#### `maxStepPercent`（最大步进百分比）
- **类型**：double
- **默认值**：`15.0`
- **范围**：1.0 ~ 50.0
- **说明**：每次风扇转速调整的最大幅度（%）。防止风扇转速剧烈波动

### 传感器配置

#### `sensorProvider`（传感器提供者）
- **类型**：string
- **默认值**：`"mock"`
- **取值**：
  - `"mock"` — 模拟传感器，返回固定温度值，用于测试
  - `"lhm"` — LibreHardwareMonitor 真实硬件传感器（需 USE_LHM=true 编译）
- **注意**：生产部署必须改为 `"lhm"`

#### `cpuSensorName` / `gpuSensorName` / `motherboardSensorName`
- **类型**：string
- **默认值**：`""` (空 = 自动匹配)
- **说明**：指定传感器名称用于绑定。留空时插件自动查找匹配的传感器
- **查找方法**：启用 debug 日志后查看 `ai-fan-plugin.log` 中 `[LHM]` 标签列出的传感器名称

#### `sensorMatchMode`（匹配模式）
- **类型**：string
- **默认值**：`"contains"`
- **取值**：
  - `"contains"` — 模糊匹配，传感器名称包含指定字符串即匹配（不区分大小写）
  - `"exact"` — 精确匹配，传感器名称必须完全一致（不区分大小写）
- **建议**：优先使用 `contains`，匹配更宽松，适合大多数硬件

### AI 调用优化配置

#### `changeThreshold`（变化阈值）
- **类型**：double
- **默认值**：`2.0`
- **范围**：0.0 ~ 20.0
- **说明**：温度变化低于此阈值（°C）时跳过 AI 调用，减少不必要的 API 请求。负载变化使用 `阈值 × 5` 作为判定标准。设为 0 表示禁用（每次轮询都调用 AI）

#### `hysteresisPercent`（迟滞死区）
- **类型**：double
- **默认值**：`3.0`
- **范围**：0.0 ~ 20.0
- **说明**：风扇转速变化低于此百分比时不实际应用，防止风扇频繁微调震荡。设为 0 表示禁用

#### `snapshotHistorySize`（快照历史数量）
- **类型**：int
- **默认值**：`5`
- **范围**：0 ~ 20
- **说明**：保留最近 N 次运行时快照供 AI 分析温度趋势，使决策更平滑。设为 0 表示不保留历史。历史数据以紧凑格式附加在 AI 提示词中

### 稳定性增强配置

#### `enableSensorSanitize`（启用传感器数据清洗）
- **类型**：bool
- **默认值**：`true`
- **说明**：启用后自动过滤异常传感器值（负温度、超过 130°C 的不可能高温、单次跳变超过 30°C 等），用上次已知正常值回退。建议保持开启，仅在调试特殊硬件时考虑关闭
- **清洗规则**：
  - 温度范围：-10°C ~ 130°C，超出用上次好值或默认 45°C 替代
  - 负载范围：0% ~ 100%，超出用上次好值或默认 50% 替代
  - 跳变检测：温度单次变化 >30°C 或负载单次变化 >60% 视为异常
  - 趋势限幅：±50 °C/min

### 诊断配置

#### `enableDiagnostics`（启用诊断）
- **类型**：bool
- **默认值**：`false`
- **说明**：启用后输出详细运行日志，便于排查问题。正常使用时建议关闭

#### `logLevel`（日志级别）
- **类型**：string
- **默认值**：`"info"`
- **取值**：`"debug"`, `"info"`, `"warning"`, `"error"`
- **级别说明**：

| 级别 | 输出内容 |
|------|----------|
| debug | 全部信息：节流判断、HTTP 详情、安全守卫修正、传感器读数 |
| info | 重要操作：启动/关闭、AI 调用结果、配置加载 |
| warning | 警告：无效模式修正、高温限制触发 |
| error | 仅错误：AI 调用异常、HTTP 失败 |

#### `logToFile`（写入日志文件）
- **类型**：bool
- **默认值**：`false`
- **说明**：启用后将日志写入 `ai-fan-plugin.log`（与插件 DLL 同目录）

## 配置建议

### 首次使用

```json
{
  "model": "gpt-4o-mini",
  "apiKey": "sk-your-key",
  "endpointUrl": "https://api.openai.com/v1/chat/completions",
  "sensorProvider": "lhm",
  "enableDiagnostics": true,
  "logLevel": "debug",
  "logToFile": true
}
```

首次运行建议开启诊断日志，确认传感器绑定和 AI 调用正常后再关闭。

### 稳定运行

```json
{
  "model": "gpt-4o",
  "apiKey": "sk-your-key",
  "endpointUrl": "https://api.openai.com/v1/chat/completions",
  "timeoutSeconds": 30,
  "temperature": 0.3,
  "pollingIntervalSeconds": 10,
  "sensorProvider": "lhm",
  "enableDiagnostics": false
}
```

### 本地 Ollama（免费方案）

```json
{
  "model": "llama3.1",
  "apiKey": "ollama",
  "endpointUrl": "http://localhost:11434/v1/chat/completions",
  "timeoutSeconds": 60,
  "temperature": 0.3,
  "sensorProvider": "lhm"
}
```

> Ollama 本地推理较慢，建议增大 `timeoutSeconds` 和 `pollingIntervalSeconds`。
