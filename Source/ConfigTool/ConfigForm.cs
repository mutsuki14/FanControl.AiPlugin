using System.Text.Json;
using FanControl.AiPlugin.Config;
using FanControl.AiPlugin.Services;
using FanControl.AiPlugin.Logging;

namespace FanControl.AiPlugin.ConfigTool;

public sealed class ConfigForm : Form
{
    // --- AI 服务字段 ---
    private readonly TextBox _txtEndpointUrl = new() { Width = 420 };
    private readonly TextBox _txtApiKey = new() { Width = 420, UseSystemPasswordChar = true };
    private readonly TextBox _txtModel = new() { Width = 200 };
    private readonly NumericUpDown _nudTemperature = new() { Minimum = 0, Maximum = 20, DecimalPlaces = 1, Increment = 0.1m, Width = 80 };
    private readonly NumericUpDown _nudTimeout = new() { Minimum = 5, Maximum = 120, Width = 80 };
    private readonly NumericUpDown _nudPollingInterval = new() { Minimum = 1, Maximum = 300, Width = 80 };
    private readonly NumericUpDown _nudMaxStep = new() { Minimum = 1, Maximum = 50, DecimalPlaces = 1, Increment = 1m, Width = 80 };

    // --- AI 调用优化字段 ---
    private readonly NumericUpDown _nudChangeThreshold = new() { Minimum = 0, Maximum = 20, DecimalPlaces = 1, Increment = 0.5m, Width = 80 };
    private readonly NumericUpDown _nudHysteresisPercent = new() { Minimum = 0, Maximum = 20, DecimalPlaces = 1, Increment = 0.5m, Width = 80 };
    private readonly NumericUpDown _nudSnapshotHistory = new() { Minimum = 0, Maximum = 20, DecimalPlaces = 0, Increment = 1m, Width = 80 };

    // --- 传感器字段 ---
    private readonly ComboBox _cboSensorProvider = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly TextBox _txtCpuSensor = new() { Width = 300 };
    private readonly TextBox _txtGpuSensor = new() { Width = 300 };
    private readonly TextBox _txtMbSensor = new() { Width = 300 };
    private readonly ComboBox _cboMatchMode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };

    // --- 稳定性增强字段 ---
    private readonly CheckBox _chkSensorSanitize = new() { Text = "启用传感器数据清洗" };

    // --- 诊断字段 ---
    private readonly CheckBox _chkDiagnostics = new() { Text = "启用诊断日志" };
    private readonly ComboBox _cboLogLevel = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly CheckBox _chkLogToFile = new() { Text = "写入日志文件" };

    // --- 按钮和状态 ---
    private readonly Button _btnTestConnection = new() { Text = "测试连接", Width = 100, Height = 32 };
    private readonly Button _btnSave = new() { Text = "保存配置", Width = 100, Height = 32 };
    private readonly Button _btnReload = new() { Text = "重新加载", Width = 100, Height = 32 };
    private readonly Button _btnOpenFile = new() { Text = "打开配置文件", Width = 110, Height = 32 };
    private readonly Label _lblStatus = new() { AutoSize = true, ForeColor = Color.DarkGray };
    private readonly CheckBox _chkShowKey = new() { Text = "显示", Width = 55, Height = 20 };

    private string _configPath = string.Empty;

    public ConfigForm()
    {
        Text = "FanControl AI 插件 — 配置工具";
        Size = new Size(560, 760);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);

        BuildLayout();
        WireEvents();
        LoadSettings();
    }

    private void BuildLayout()
    {
        var tabControl = new TabControl { Dock = DockStyle.Fill };

        // ===== Tab 1: AI 服务 =====
        var tabAi = new TabPage("AI 服务") { Padding = new Padding(10) };
        var panelAi = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            Padding = new Padding(5)
        };
        panelAi.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        panelAi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(panelAi, "端点 URL:", _txtEndpointUrl);
        // API Key 行：TextBox + 显示复选框
        var apiKeyPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight };
        apiKeyPanel.Controls.Add(_txtApiKey);
        apiKeyPanel.Controls.Add(_chkShowKey);
        AddRow(panelAi, "API Key:", apiKeyPanel);
        AddRow(panelAi, "模型:", _txtModel);
        AddRow(panelAi, "温度:", _nudTemperature);
        AddRow(panelAi, "超时(秒):", _nudTimeout);
        AddRow(panelAi, "轮询间隔(秒):", _nudPollingInterval);
        AddRow(panelAi, "最大步进(%):", _nudMaxStep);
        AddRow(panelAi, "变化阈值(°C):", _nudChangeThreshold);
        AddRow(panelAi, "迟滞死区(%):", _nudHysteresisPercent);
        AddRow(panelAi, "快照历史数:", _nudSnapshotHistory);

        // 测试连接按钮
        var testPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        testPanel.Controls.Add(_btnTestConnection);
        testPanel.Controls.Add(_lblStatus);
        AddRow(panelAi, "", testPanel);

        tabAi.Controls.Add(panelAi);

        // ===== Tab 2: 传感器 =====
        var tabSensor = new TabPage("传感器") { Padding = new Padding(10) };
        var panelSensor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            Padding = new Padding(5)
        };
        panelSensor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        panelSensor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _cboSensorProvider.Items.AddRange(new object[] { "mock", "lhm" });
        AddRow(panelSensor, "传感器提供者:", _cboSensorProvider);
        AddRow(panelSensor, "CPU 传感器名:", _txtCpuSensor);
        AddRow(panelSensor, "GPU 传感器名:", _txtGpuSensor);
        AddRow(panelSensor, "主板传感器名:", _txtMbSensor);

        _cboMatchMode.Items.AddRange(new object[] { "contains", "exact" });
        AddRow(panelSensor, "匹配模式:", _cboMatchMode);
        AddRow(panelSensor, "", _chkSensorSanitize);

        // 传感器说明
        var lblSensorHelp = new Label
        {
            Text = "提示：传感器名称留空表示自动匹配。启用 debug 日志可查看所有可用传感器名。\n仅在 sensorProvider=lhm 且以 -p:USE_LHM=true 编译时生效。",
            AutoSize = true,
            ForeColor = Color.Gray,
            Padding = new Padding(0, 10, 0, 0)
        };
        panelSensor.SetColumnSpan(lblSensorHelp, 2);
        panelSensor.Controls.Add(lblSensorHelp);

        tabSensor.Controls.Add(panelSensor);

        // ===== Tab 3: 诊断 =====
        var tabDiag = new TabPage("诊断") { Padding = new Padding(10) };
        var panelDiag = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            Padding = new Padding(5)
        };
        panelDiag.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        panelDiag.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(panelDiag, "", _chkDiagnostics);

        _cboLogLevel.Items.AddRange(new object[] { "debug", "info", "warning", "error" });
        AddRow(panelDiag, "日志级别:", _cboLogLevel);
        AddRow(panelDiag, "", _chkLogToFile);

        tabDiag.Controls.Add(panelDiag);

        tabControl.TabPages.AddRange(new[] { tabAi, tabSensor, tabDiag });

        // ===== 底部按钮栏 =====
        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(5)
        };
        bottomPanel.Controls.Add(_btnSave);
        bottomPanel.Controls.Add(_btnReload);
        bottomPanel.Controls.Add(_btnOpenFile);

        Controls.Add(tabControl);
        Controls.Add(bottomPanel);
    }

    private static void AddRow(TableLayoutPanel panel, string label, Control control)
    {
        var row = panel.RowCount;
        panel.RowCount = row + 1;
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        if (!string.IsNullOrEmpty(label))
        {
            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 6, 0, 0)
            };
            panel.Controls.Add(lbl, 0, row);
        }

        control.Anchor = AnchorStyles.Left;
        panel.Controls.Add(control, 1, row);
    }

    private void WireEvents()
    {
        _btnSave.Click += (_, _) => SaveSettings();
        _btnReload.Click += (_, _) => LoadSettings();
        _btnTestConnection.Click += async (_, _) => await TestConnectionAsync();
        _btnOpenFile.Click += (_, _) => OpenConfigFile();
        _chkShowKey.CheckedChanged += (_, _) =>
        {
            _txtApiKey.UseSystemPasswordChar = !_chkShowKey.Checked;
        };
    }

    private void LoadSettings()
    {
        try
        {
            _configPath = SettingsStore.GetFilePath();
            var settings = SettingsStore.Load();

            _txtEndpointUrl.Text = settings.EndpointUrl;
            _txtApiKey.Text = settings.ApiKey;
            _txtModel.Text = settings.Model;
            _nudTemperature.Value = (decimal)Math.Clamp(settings.Temperature, 0, 2.0);
            _nudTimeout.Value = Math.Clamp(settings.TimeoutSeconds, 5, 120);
            _nudPollingInterval.Value = Math.Clamp(settings.PollingIntervalSeconds, 1, 300);
            _nudMaxStep.Value = (decimal)Math.Clamp(settings.MaxStepPercent, 1, 50);
            _nudChangeThreshold.Value = (decimal)Math.Clamp(settings.ChangeThreshold, 0, 20);
            _nudHysteresisPercent.Value = (decimal)Math.Clamp(settings.HysteresisPercent, 0, 20);
            _nudSnapshotHistory.Value = Math.Clamp(settings.SnapshotHistorySize, 0, 20);

            _cboSensorProvider.SelectedItem = settings.SensorProvider;
            _txtCpuSensor.Text = settings.CpuSensorName;
            _txtGpuSensor.Text = settings.GpuSensorName;
            _txtMbSensor.Text = settings.MotherboardSensorName;
            _cboMatchMode.SelectedItem = settings.SensorMatchMode;

            _chkSensorSanitize.Checked = settings.EnableSensorSanitize;

            _chkDiagnostics.Checked = settings.EnableDiagnostics;
            _cboLogLevel.SelectedItem = settings.LogLevel;
            _chkLogToFile.Checked = settings.LogToFile;

            SetStatus($"配置已加载: {_configPath}", Color.DarkGreen);
        }
        catch (Exception ex)
        {
            SetStatus($"加载失败: {ex.Message}", Color.Red);
        }
    }

    private void SaveSettings()
    {
        try
        {
            var settings = new AiProviderSettings
            {
                EndpointUrl = _txtEndpointUrl.Text.Trim(),
                ApiKey = _txtApiKey.Text.Trim(),
                Model = _txtModel.Text.Trim(),
                Temperature = (double)_nudTemperature.Value,
                TimeoutSeconds = (int)_nudTimeout.Value,
                PollingIntervalSeconds = (int)_nudPollingInterval.Value,
                MaxStepPercent = (double)_nudMaxStep.Value,
                ChangeThreshold = (double)_nudChangeThreshold.Value,
                HysteresisPercent = (double)_nudHysteresisPercent.Value,
                SnapshotHistorySize = (int)_nudSnapshotHistory.Value,
                SensorProvider = _cboSensorProvider.SelectedItem?.ToString() ?? "mock",
                CpuSensorName = _txtCpuSensor.Text.Trim(),
                GpuSensorName = _txtGpuSensor.Text.Trim(),
                MotherboardSensorName = _txtMbSensor.Text.Trim(),
                SensorMatchMode = _cboMatchMode.SelectedItem?.ToString() ?? "contains",
                EnableSensorSanitize = _chkSensorSanitize.Checked,
                EnableDiagnostics = _chkDiagnostics.Checked,
                LogLevel = _cboLogLevel.SelectedItem?.ToString() ?? "info",
                LogToFile = _chkLogToFile.Checked
            };

            SettingsStore.Save(settings, _configPath);
            SetStatus($"配置已保存: {_configPath}", Color.DarkGreen);
        }
        catch (Exception ex)
        {
            SetStatus($"保存失败: {ex.Message}", Color.Red);
        }
    }

    private async Task TestConnectionAsync()
    {
        _btnTestConnection.Enabled = false;
        SetStatus("正在测试连接...", Color.DarkBlue);

        try
        {
            var settings = new AiProviderSettings
            {
                EndpointUrl = _txtEndpointUrl.Text.Trim(),
                ApiKey = _txtApiKey.Text.Trim(),
                Model = _txtModel.Text.Trim(),
                Temperature = (double)_nudTemperature.Value,
                TimeoutSeconds = (int)_nudTimeout.Value
            };

            if (!settings.IsValid())
            {
                SetStatus("配置无效: 请检查端点 URL、API Key 和模型名是否已填写", Color.Red);
                return;
            }

            var client = new OpenAiCompatibleClient(settings);
            var (success, message) = await client.TestConnectionAsync();

            if (success)
            {
                SetStatus($"连接成功: {TruncateMessage(message, 60)}", Color.DarkGreen);
            }
            else
            {
                SetStatus($"连接失败: {TruncateMessage(message, 80)}", Color.Red);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"测试异常: {TruncateMessage(ex.Message, 80)}", Color.Red);
        }
        finally
        {
            _btnTestConnection.Enabled = true;
        }
    }

    private void OpenConfigFile()
    {
        if (string.IsNullOrEmpty(_configPath) || !File.Exists(_configPath))
        {
            SetStatus("配置文件不存在", Color.Red);
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _configPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            SetStatus($"打开失败: {ex.Message}", Color.Red);
        }
    }

    private void SetStatus(string text, Color color)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetStatus(text, color));
            return;
        }
        _lblStatus.Text = text;
        _lblStatus.ForeColor = color;
    }

    private static string TruncateMessage(string msg, int maxLen)
    {
        if (string.IsNullOrEmpty(msg)) return msg;
        msg = msg.Replace('\n', ' ').Replace('\r', ' ');
        return msg.Length <= maxLen ? msg : msg[..maxLen] + "...";
    }
}
