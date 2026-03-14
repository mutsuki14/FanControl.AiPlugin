# 更新说明

## 版本历史

---

### v7 — StabilityEnhancement（稳定性增强版）
**日期**：2026-03-13

**新增**：
- `SensorSanitizer` 传感器数据清洗模块：自动过滤异常传感器值（负温度、不可能高温、跳变等），用上次已知正常值回退
- `enableSensorSanitize` 配置项：控制传感器清洗开关（默认开启）
- 智能本地回退增强：AI 不可用时基于快照历史趋势修正 + 上次安全决策平滑混合（70% 新 + 30% 旧），避免风扇突变

**改进**：
- `AiDecisionService` 现在将 `lastDecision` 传递给 `LocalFallback`，回退决策可参考上次安全值
- `FanControlPluginAdapter` 三个 tick 方法均集成传感器清洗管线（Collect → Sanitize → 后续处理）
- 配置工具新增「启用传感器数据清洗」复选框（传感器标签页）

**清洗规则**：
- 温度范围：-10°C ~ 130°C，超出用上次好值或默认 45°C 替代
- 负载范围：0% ~ 100%，超出用上次好值或默认 50% 替代
- 跳变检测：温度单次变化 >30°C 或负载单次变化 >60% 视为异常
- 趋势限幅：±50 °C/min
- 风扇百分比：0% ~ 100%

---

### v6 — AiOptimization（AI 机制优化版）
**日期**：2026-03-13

**新增**：
- `changeThreshold` 变化阈值：温度/负载变化低于阈值时跳过 AI 调用，减少 API 请求
- `hysteresisPercent` 迟滞死区：风扇转速微小变化不实际应用，防止频繁震荡
- `snapshotHistorySize` 快照历史：保留最近 N 次快照供 AI 趋势分析，决策更平滑
- 更严格的 AI JSON 输出约束（字段类型、范围、数量、禁止 markdown）
- 配置工具新增 3 个优化参数编辑控件

**改进**：
- AI 提示词增加历史快照上下文和决策连续性原则
- 安全守卫后增加迟滞过滤，减少风扇微调噪音
- 变化检测在温度和负载两个维度独立判断

---

### v5 — ReleasePack（发布包版）
**日期**：2026-03-13

**新增**：
- 发布包目录结构（Plugin/、ConfigTool/、Docs/、Scripts/、Source/）
- 完整中文文档集（快速开始、部署步骤、配置说明、故障排查、更新说明）
- Windows 批处理启动脚本（编译脚本、配置工具启动脚本）
- 首次使用检查清单
- 插件部署说明文件及默认配置

**改进**：
- 整体目录结构面向最终交付优化
- README 改为面向用户的发布总说明

---

### v4 — ConfigUI（配置界面版）
**日期**：2026-03-13

**新增**：
- WinForms 可视化配置工具（`FanControl.AiPlugin.ConfigTool.exe`）
- 三标签页配置界面：AI 服务、传感器、诊断
- 全部 15 个配置字段的图形化编辑
- 测试连接功能（验证 AI 端点/Key/模型）
- API Key 密文显示/切换
- 配置文件一键打开

**修复**：
- SettingsStore 添加 `JsonNamingPolicy.CamelCase`，修复 JSON 保存时字段名大小写问题

---

### v3 — SensorBindings（传感器绑定版）
**日期**：2026-03-13

**新增**：
- 传感器名称绑定功能（`cpuSensorName`、`gpuSensorName`、`motherboardSensorName`）
- 两种匹配模式：`contains`（模糊）和 `exact`（精确）
- `SensorBindingConfig` / `SensorBindingResult` 数据模型
- 传感器绑定诊断信息输出

**改进**：
- 中文 README 文档

---

### v2 — Diagnostics（诊断增强版）

**新增**：
- PluginLogger 日志系统（控制台 + 文件输出）
- DiagnosticsSummary 诊断摘要导出
- 4 级日志级别（debug/info/warning/error）
- `enableDiagnostics`、`logLevel`、`logToFile` 配置字段

**改进**：
- 全面的运行时状态追踪
- 格式化的日志输出（时间戳、级别、标签）

---

### v1 — 基础版

**功能**：
- FanControl 插件接口实现（IPlugin2、IPluginSensor、IPluginControlSensor）
- OpenAI 兼容 AI 决策服务
- 多层安全机制（限幅、紧急、高温保底、趋势预判、步进限制）
- Mock 传感器 + LibreHardwareMonitor 真实传感器
- 条件编译支持 USE_LHM
- Demo 控制台

---

## 版本演进路线

```
v1 基础版
 └─ v2 诊断增强版（+日志系统）
     └─ v3 传感器绑定版（+名称绑定）
         └─ v4 配置界面版（+WinForms 配置工具）
             └─ v5 发布包版（+文档/脚本/部署结构）
                 └─ v6 AI 机制优化版（+变化阈值/迟滞/快照历史）
                     └─ v7 稳定性增强版（+传感器清洗/智能回退）  ← 当前版本
```

## 未来计划

- 插件内置自动更新检查
- 配置热重载（无需重启 FanControl）
- 风扇曲线可视化
- 多语言支持（英文 README/文档）
- CI/CD 自动构建和发布
