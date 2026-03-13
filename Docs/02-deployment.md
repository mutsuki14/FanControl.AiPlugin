# 部署步骤

本文档详细说明如何将 FanControl AI 插件部署到 Windows 系统上。

## 系统要求

| 项目 | 要求 |
|------|------|
| 操作系统 | Windows 10 1809+ / Windows 11 |
| 运行时 | .NET 8.0 运行时 + Windows 桌面运行时 |
| FanControl | v1.4.0+（支持 IPlugin2 接口） |
| 权限 | 管理员（读取硬件传感器需要） |
| AI 服务 | 任意 OpenAI Chat Completions 兼容接口 |

## 推荐目录结构

```
C:\FanControlAI\                        ← 推荐工作根目录
├── FanControl\                         ← FanControl 主程序
│   ├── FanControl.exe
│   └── Plugins\                        ← 插件部署位置
│       ├── FanControl.AiPlugin.dll     ← 插件主 DLL
│       ├── LibreHardwareMonitorLib.dll  ← 传感器库
│       ├── HidSharp.dll                ← 传感器库依赖
│       └── ai-fan-settings.json        ← 配置文件
├── ConfigTool\                         ← 配置工具（可选位置）
│   ├── FanControl.AiPlugin.ConfigTool.exe
│   └── ...
└── Logs\                               ← 日志目录（自动生成）
    └── ai-fan-plugin.log
```

## 部署流程

### 1. 编译源码

```cmd
:: 进入源码目录
cd Source

:: 编译全部项目（插件 + 配置工具）
dotnet build -c Release -p:USE_LHM=true

:: 仅编译插件
dotnet build FanControl.AiPlugin.csproj -c Release -p:USE_LHM=true

:: 仅编译配置工具
dotnet build ConfigTool\FanControl.AiPlugin.ConfigTool.csproj -c Release
```

### 2. 复制插件文件

从编译输出复制到 FanControl Plugins 目录：

```cmd
:: 设置路径（根据实际情况修改）
set FANCONTROL_PLUGINS=C:\Program Files\FanControl\Plugins
set BUILD_OUT=Source\bin\Release\net8.0

:: 复制插件 DLL
copy "%BUILD_OUT%\FanControl.AiPlugin.dll" "%FANCONTROL_PLUGINS%\"

:: 复制传感器库（USE_LHM=true 编译时）
copy "%BUILD_OUT%\LibreHardwareMonitorLib.dll" "%FANCONTROL_PLUGINS%\"
copy "%BUILD_OUT%\HidSharp.dll" "%FANCONTROL_PLUGINS%\"

:: 复制默认配置（如果是首次部署）
copy "Plugin\ai-fan-settings.json" "%FANCONTROL_PLUGINS%\"
```

### 3. 配置 AI 服务

编辑 `%FANCONTROL_PLUGINS%\ai-fan-settings.json`，必须修改的字段：

| 字段 | 示例值 | 说明 |
|------|--------|------|
| `apiKey` | `sk-xxx...` | 你的 API Key |
| `endpointUrl` | `https://api.openai.com/v1/chat/completions` | AI 服务端点 |
| `model` | `gpt-4o` | 模型名称 |
| `sensorProvider` | `lhm` | 改为 lhm 以使用真实传感器 |

### 4. 部署配置工具（可选）

```cmd
:: 配置工具编译输出
set CONFIG_OUT=Source\ConfigTool\bin\Release\net8.0-windows

:: 复制到独立目录
xcopy "%CONFIG_OUT%\*" "C:\FanControlAI\ConfigTool\" /E /Y
```

### 5. 启动验证

1. 以管理员身份启动 FanControl
2. 检查插件是否加载（FanControl 传感器/控制列表中出现 AI 相关条目）
3. 如果未加载，启用诊断日志排查

## 升级部署

1. 关闭 FanControl
2. 备份当前 `ai-fan-settings.json`
3. 覆盖插件 DLL 文件
4. 恢复或合并配置文件
5. 重启 FanControl

## 卸载

1. 关闭 FanControl
2. 删除 Plugins 目录中的以下文件：
   - `FanControl.AiPlugin.dll`
   - `ai-fan-settings.json`
   - `ai-fan-plugin.log`（如果存在）
3. 如果其他插件不使用，也可删除 `LibreHardwareMonitorLib.dll` 和 `HidSharp.dll`
4. 重启 FanControl

## 多机部署

配置文件 `ai-fan-settings.json` 可在多台机器间共享。注意：
- `cpuSensorName` / `gpuSensorName` / `motherboardSensorName` 可能因硬件不同而需要调整
- API Key 相同即可共用
- 首次部署建议每台机器独立运行配置工具的"测试连接"验证
