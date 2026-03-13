# Plugin 目录 — 插件部署文件

本目录用于存放 FanControl AI 插件的发布文件。

## 编译后应放置的文件

编译完成后，需要将以下文件复制到本目录：

```
Plugin/
├── FanControl.AiPlugin.dll           ← 编译产物（必需）
├── LibreHardwareMonitorLib.dll        ← 编译产物（USE_LHM=true 时必需）
├── HidSharp.dll                       ← LibreHardwareMonitorLib 依赖（USE_LHM=true 时必需）
├── ai-fan-settings.json               ← 默认配置（已包含，需编辑）
└── README_PLUGIN.md                   ← 本说明
```

## 编译命令

```bash
cd Source
dotnet build -c Release -p:USE_LHM=true
```

编译产物位于：`Source/bin/Release/net8.0/`

## 部署步骤

1. 编辑 `ai-fan-settings.json`，填入你的 AI 服务信息：
   - `apiKey`：你的 API Key
   - `endpointUrl`：AI 服务端点
   - `model`：模型名称
   - `sensorProvider`：改为 `"lhm"` 以使用真实硬件传感器

2. 将本目录下所有 DLL 和 JSON 文件复制到 FanControl 的 `Plugins` 目录：
   ```
   C:\Program Files\FanControl\Plugins\
   ```

3. 重启 FanControl

## FanControl Plugins 目录位置

默认路径：`C:\Program Files\FanControl\Plugins\`

如果你使用便携版 FanControl，Plugins 目录在 FanControl.exe 同级目录下。

## 注意事项

- 插件需要 .NET 8.0 运行时
- 编译时需要 `FanControl.Plugins.dll`（FanControl 提供的插件接口 DLL），放在 Source 的上层目录
- 使用真实传感器需以管理员权限运行 FanControl
- 配置文件 `ai-fan-settings.json` 需与插件 DLL 放在同一目录
