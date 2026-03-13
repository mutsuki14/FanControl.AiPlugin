# 更新说明

## 版本历史

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
             └─ v5 发布包版（+文档/脚本/部署结构）  ← 当前版本
```

## 未来计划

- 插件内置自动更新检查
- 配置热重载（无需重启 FanControl）
- 风扇曲线可视化
- 多语言支持（英文 README/文档）
- CI/CD 自动构建和发布
