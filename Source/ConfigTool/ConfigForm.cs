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

    // --- 诊断字段 ---
    private readonly CheckBox _chkDiagnostics = new() { Text = "\u542f\u7528\u8bca\u65ad\u65e5\u5fd7" };
    private readonly ComboBox _cboLogLevel = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly CheckBox _chkLogToFile = new() { Text = "\u5199\u5165\u65e5\u5fd7\u6587\u4ef6" };

    // --- 按钮和状态 ---
    private readonly Button _btnTestConnection = new() { Text = "\u6d4b\u8bd5\u8fde\u63a5", Width = 100, Height = 32 };
    private readonly Button _btnSave = new() { Text = "\u4fdd\u5b58\u914d\u7f6e", Width = 100, Height = 32 };
    private readonly Button _btnReload = new() { Text = "\u91cd\u65b0\u52a0\u8f7d", Width = 100, Height = 32 };
    private readonly Button _btnOpenFile = new() { Text = "\u6253\u5f00\u914d\u7f6e\u6587\u4ef6", Width = 110, Height = 32 };
    private readonly Label _lblStatus = new() { AutoSize = true, ForeColor = Color.DarkGray };
    private readonly CheckBox _chkShowKey = new() { Text = "\u663e\u793a", Width = 55, Height = 20 };

    private string _configPath = string.Empty;

    public ConfigForm()
    {
        Text = "FanControl AI \u63d2\u4ef6 \u2014 \u914d\u7f6e\u5de5\u5177";
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
        var tabAi = new TabPage("AI \u670d\u52a1") { Padding = new Padding(10) };
        var panelAi = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            Padding = new Padding(5)
        };
        panelAi.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        panelAi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(panelAi, "\u7aef\u70b9 URL:", _txtEndpointUrl);
        // API Key 行：TextBox + 显示复选框
        var apiKeyPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight };
        apiKeyPanel.Controls.Add(_txtApiKey);
        apiKeyPanel.Controls.Add(_chkShowKey);
        AddRow(panelAi, "API Key:", apiKeyPanel);
        AddRow(panelAi, "\u6a21\u578b:", _txtModel);
        AddRow(panelAi, "\u6e29\u5ea6:", _nudTemperature);
        AddRow(panelAi, "\u8d85\u65f6(\u79d2):", _nudTimeout);
        AddRow(panelAi, "\u8f6e\u8be2\u95f4\u9694(\u79d2):", _nudPollingInterval);
        AddRow(panelAi, "\u6700\u5927\u6b65\u8fdb(%):", _nudMaxStep);
        AddRow(panelAi, "\u53d8\u5316\u9608\u503c(\u00b0C):", _nudChangeThreshold);
        AddRow(panelAi, "\u8fdf\u6ede\u6b7b\u533a(%):", _nudHysteresisPercent);
        AddRow(panelAi, "\u5feb\u7167\u5386\u53f2\u6570:", _nudSnapshotHistory);

        // 测试连接按钮
        var testPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        testPanel.Controls.Add(_btnTestConnection);
        testPanel.Controls.Add(_lblStatus);
        AddRow(panelAi, "", testPanel);

        tabAi.Controls.Add(panelAi);

        // ===== Tab 2: 传感器 =====
        var tabSensor = new TabPage("\u4f20\u611f\u5668") { Padding = new Padding(10) };
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
        AddRow(panelSensor, "\u4f20\u611f\u5668\u63d0\u4f9b\u8005:", _cboSensorProvider);
        AddRow(panelSensor, "CPU \u4f20\u611f\u5668\u540d:", _txtCpuSensor);
        AddRow(panelSensor, "GPU \u4f20\u611f\u5668\u540d:", _txtGpuSensor);
        AddRow(panelSensor, "\u4e3b\u677f\u4f20\u611f\u5668\u540d:", _txtMbSensor);

        _cboMatchMode.Items.AddRange(new object[] { "contains", "exact" });
        AddRow(panelSensor, "\u5339\u914d\u6a21\u5f0f:", _cboMatchMode);

        // 传感器说明
        var lblSensorHelp = new Label
        {
            Text = "\u63d0\u793a\uff1a\u4f20\u611f\u5668\u540d\u79f0\u7559\u7a7a\u8868\u793a\u81ea\u52a8\u5339\u914d\u3002\u542f\u7528 debug \u65e5\u5fd7\u53ef\u67e5\u770b\u6240\u6709\u53ef\u7528\u4f20\u611f\u5668\u540d\u3002\n\u4ec5\u5728 sensorProvider=lhm \u4e14\u4ee5 -p:USE_LHM=true \u7f16\u8bd1\u65f6\u751f\u6548\u3002",
            AutoSize = true,
            ForeColor = Color.Gray,
            Padding = new Padding(0, 10, 0, 0)
        };
        panelSensor.SetColumnSpan(lblSensorHelp, 2);
        panelSensor.Controls.Add(lblSensorHelp);

        tabSensor.Controls.Add(panelSensor);

        // ===== Tab 3: 诊断 =====
        var tabDiag = new TabPage("\u8bca\u65ad") { Padding = new Padding(10) };
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
        AddRow(panelDiag, "\u65e5\u5fd7\u7ea7\u522b:", _cboLogLevel);
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

            _chkDiagnostics.Checked = settings.EnableDiagnostics;
            _cboLogLevel.SelectedItem = settings.LogLevel;
            _chkLogToFile.Checked = settings.LogToFile;

            SetStatus($"\u914d\u7f6e\u5df2\u52a0\u8f7d: {_configPath}", Color.DarkGreen);
        }
        catch (Exception ex)
        {
            SetStatus($"\u52a0\u8f7d\u5931\u8d25: {ex.Message}", Color.Red);
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
                EnableDiagnostics = _chkDiagnostics.Checked,
                LogLevel = _cboLogLevel.SelectedItem?.ToString() ?? "info",
                LogToFile = _chkLogToFile.Checked
            };

            SettingsStore.Save(settings, _configPath);
            SetStatus($"\u914d\u7f6e\u5df2\u4fdd\u5b58: {_configPath}", Color.DarkGreen);
        }
        catch (Exception ex)
        {
            SetStatus($"\u4fdd\u5b58\u5931\u8d25: {ex.Message}", Color.Red);
        }
    }

    private async Task TestConnectionAsync()
    {
        _btnTestConnection.Enabled = false;
        SetStatus("\u6b63\u5728\u6d4b\u8bd5\u8fde\u63a5...", Color.DarkBlue);

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
                SetStatus("\u914d\u7f6e\u65e0\u6548: \u8bf7\u68c0\u67e5\u7aef\u70b9 URL\u3001API Key \u548c\u6a21\u578b\u540d\u662f\u5426\u5df2\u586b\u5199", Color.Red);
                return;
            }

            var client = new OpenAiCompatibleClient(settings);
            var (success, message) = await client.TestConnectionAsync();

            if (success)
            {
                SetStatus($"\u8fde\u63a5\u6210\u529f: {TruncateMessage(message, 60)}", Color.DarkGreen);
            }
            else
            {
                SetStatus($"\u8fde\u63a5\u5931\u8d25: {TruncateMessage(message, 80)}", Color.Red);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"\u6d4b\u8bd5\u5f02\u5e38: {TruncateMessage(ex.Message, 80)}", Color.Red);
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
            SetStatus("\u914d\u7f6e\u6587\u4ef6\u4e0d\u5b58\u5728", Color.Red);
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
            SetStatus($"\u6253\u5f00\u5931\u8d25: {ex.Message}", Color.Red);
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
