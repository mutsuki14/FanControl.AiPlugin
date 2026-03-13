# 故障排查

本文档列出常见问题及其解决方案。

## 诊断工具

遇到问题时，首先启用诊断日志：

```json
{
  "enableDiagnostics": true,
  "logLevel": "debug",
  "logToFile": true
}
```

修改后重启 FanControl，查看 Plugins 目录下的 `ai-fan-plugin.log`。

---

## 插件加载问题

### 插件未出现在 FanControl 中

**症状**：FanControl 的传感器/控制列表中没有 AI 相关条目。

**排查步骤**：
1. 确认 `FanControl.AiPlugin.dll` 在 FanControl 的 `Plugins` 目录中
2. 确认已安装 .NET 8.0 运行时
3. 检查 FanControl 版本是否支持 IPlugin2（v1.4.0+）
4. 查看 Windows 事件查看器中是否有 DLL 加载错误

**常见原因**：
- DLL 文件缺失（特别是 `LibreHardwareMonitorLib.dll`）
- .NET 运行时版本不匹配
- FanControl 版本过旧

### 插件加载后崩溃

**排查步骤**：
1. 检查 `ai-fan-settings.json` 是否在 DLL 同目录
2. 验证 JSON 格式是否正确（可用配置工具重新保存一次）
3. 启用诊断日志查看最后一条日志

---

## AI 服务问题

### 测试连接失败：超时

**可能原因**：
- 网络不通或有代理/防火墙
- 端点 URL 错误
- AI 服务不可用

**解决方案**：
1. 浏览器中直接访问端点 URL 确认可达
2. 检查系统代理设置
3. 增大 `timeoutSeconds`（如 60 秒）
4. 本地 Ollama：确认 `ollama serve` 已启动

### 测试连接失败：401 Unauthorized

**原因**：API Key 无效。

**解决方案**：
1. 检查 API Key 是否正确（注意不要有多余空格）
2. 确认 API Key 未过期或被禁用
3. Ollama 无需有效 Key，填任意值即可

### 测试连接失败：404 Not Found

**原因**：端点 URL 不正确。

**解决方案**：
1. 确认 URL 路径完整（需包含 `/v1/chat/completions`）
2. Azure OpenAI 需包含 `api-version` 参数
3. 检查 URL 中是否有拼写错误

### AI 返回结果不稳定

**解决方案**：
1. 降低 `temperature` 值（建议 0.1~0.3）
2. 减小 `maxStepPercent`（如 10%）
3. 增大 `pollingIntervalSeconds`（如 10 秒）

---

## 传感器问题

### 传感器温度始终为 0

**可能原因**：
1. `sensorProvider` 设为 `"mock"`（模拟模式固定返回测试值）
2. 未以管理员权限运行 FanControl
3. 编译时未启用 `USE_LHM=true`

**解决方案**：
1. 将 `sensorProvider` 改为 `"lhm"`
2. 以管理员身份运行 FanControl
3. 重新编译：`dotnet build -c Release -p:USE_LHM=true`

### 传感器名称未匹配

**症状**：日志中显示"未找到匹配的传感器"。

**排查步骤**：
1. 启用 `debug` 级别日志
2. 查看日志中 `[LHM]` 标签列出的所有可用传感器名称
3. 将正确的名称填入 `cpuSensorName` / `gpuSensorName` / `motherboardSensorName`
4. 先尝试 `"contains"` 模式（模糊匹配）
5. 如果有多个同名传感器，再切换到 `"exact"` 模式

### 传感器读数异常

**可能原因**：
- LibreHardwareMonitor 不完全支持你的硬件
- 传感器名称绑定到了错误的传感器

**解决方案**：
1. 用 `debug` 日志查看实际绑定的传感器及其读数
2. 更换传感器名称
3. 尝试使用 LibreHardwareMonitor 独立程序确认传感器可读

---

## 配置工具问题

### 配置工具找不到配置文件

**解决方案**：
1. 将 `ai-fan-settings.json` 放到配置工具 EXE 同目录
2. 用命令行指定路径：`ConfigTool.exe "C:\path\to\ai-fan-settings.json"`
3. 配置工具会自动创建默认配置文件

### 配置保存后插件未生效

**原因**：插件在 FanControl 启动时加载配置，运行中不会重新读取。

**解决方案**：
1. 保存配置后重启 FanControl
2. 确认配置工具保存的文件路径与插件读取的路径一致

### 配置工具无法启动

**可能原因**：
- 缺少 .NET 8.0 Windows 桌面运行时
- Windows 版本过旧

**解决方案**：
1. 安装 [.NET 8.0 Windows 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0)
2. 确认 Windows 版本为 10 1809 或更新

---

## 安全机制触发

### 风扇始终全速运转

**可能原因**：紧急保护触发（温度 >= 95 度C）。

**排查**：
1. 检查实际温度是否异常
2. 查看日志中安全守卫的触发记录
3. 如果温度正常但传感器读数异常，检查传感器绑定

### 风扇转速不低于 70%

**原因**：高温保底机制触发（CPU >= 80 度C / GPU >= 85 度C / 主板 >= 55 度C）。

**说明**：这是安全设计，不建议关闭。如果温度确实正常但传感器读数不对，排查传感器绑定问题。

---

## 日志分析

### 关键日志标签

| 标签 | 含义 |
|------|------|
| `[Plugin]` | 插件生命周期（Load/Close） |
| `[Config]` | 配置加载 |
| `[HTTP]` | AI API 请求和响应 |
| `[Safety]` | 安全守卫触发 |
| `[LHM]` | LibreHardwareMonitor 传感器操作 |
| `[Bind]` | 传感器名称绑定 |
| `[AI]` | AI 决策结果 |

### 日志位置

- 插件日志：`<Plugins目录>\ai-fan-plugin.log`
- 控制台输出：运行 Demo 时在命令行窗口可见
