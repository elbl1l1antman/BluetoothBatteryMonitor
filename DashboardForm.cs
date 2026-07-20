namespace BluetoothBatteryMonitor;

internal sealed class DashboardForm : Form
{
    private readonly DeviceListControl _deviceList = new() { Location = new Point(258, 166), Size = new Size(784, 470) };
    private readonly Label _title = LabelAt("연결된 기기", 258, 54, 430, 48, 25, FontStyle.Bold, Theme.Text);
    private readonly Label _summary = LabelAt("확인 중…", 260, 104, 500, 27, 11, FontStyle.Regular, Theme.Muted);
    private readonly Button _refreshButton;
    private readonly Panel _settingsPanel;
    private readonly Panel _widgetPanel;
    private readonly Button _devicesNav;
    private readonly Button _widgetNav;
    private readonly Button _settingsNav;
    private CheckBox _startup = null!;
    private CheckBox _showWidget = null!;
    private CheckBox _lowBattery = null!;
    private ComboBox _threshold = null!;

    public event EventHandler? RefreshRequested;
    public event EventHandler<AppSettings>? SettingsChanged;
    public event EventHandler<(string DeviceId, bool Selected)>? TaskbarSelectionChanged;

    public DashboardForm(AppSettings settings, bool demoMode)
    {
        Text = demoMode ? "Battery Dock · 데모 모드" : "Battery Dock";
        ClientSize = new Size(1080, 700);
        MinimumSize = new Size(1000, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        HandleCreated += (_, _) => NativeWindowStyle.ApplyDark(this);

        var sidebar = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = Theme.Sidebar };
        sidebar.Controls.Add(LabelAt("▰", 28, 24, 35, 36, 17, FontStyle.Bold, Theme.Green));
        sidebar.Controls.Add(LabelAt("Battery Dock", 64, 26, 140, 34, 13, FontStyle.Bold, Theme.Text));
        sidebar.Controls.Add(LabelAt("v0.1.0", 26, 638, 100, 24, 8.5f, FontStyle.Regular, Theme.Muted));
        _devicesNav = CreateNavButton("▱   기기", 86, true);
        _widgetNav = CreateNavButton("⊞   위젯", 146, false);
        _settingsNav = CreateNavButton("⚙   설정", 206, false);
        sidebar.Controls.AddRange([_devicesNav, _widgetNav, _settingsNav]);
        Controls.Add(sidebar);

        _refreshButton = new Button
        {
            Text = "↻  새로 고침", Location = new Point(890, 70), Size = new Size(150, 46),
            FlatStyle = FlatStyle.Flat, BackColor = Theme.Surface, ForeColor = Theme.Text,
            Font = new Font("Segoe UI", 10), Cursor = Cursors.Hand
        };
        _refreshButton.FlatAppearance.BorderColor = Theme.Border;
        _refreshButton.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        _deviceList.TaskbarSelectionChanged += (_, change) => TaskbarSelectionChanged?.Invoke(this, change);

        _settingsPanel = BuildSettingsPanel(settings);
        _widgetPanel = BuildWidgetPanel();
        Controls.AddRange([_title, _summary, _refreshButton, _deviceList, _settingsPanel, _widgetPanel]);

        _devicesNav.Click += (_, _) => ShowPage("devices");
        _widgetNav.Click += (_, _) => ShowPage("widget");
        _settingsNav.Click += (_, _) => ShowPage("settings");
        Resize += (_, _) => Reflow();
        FormClosing += (_, e) =>
        {
            if (e.CloseReason != CloseReason.ApplicationExitCall) { e.Cancel = true; Hide(); }
        };
    }

    public void SetDevices(IReadOnlyList<BatteryDevice> devices, IEnumerable<string>? taskbarDeviceIds = null, bool isError = false)
    {
        _deviceList.SetDevices(devices, taskbarDeviceIds);
        _summary.Text = isError ? "배터리 정보를 가져오지 못했습니다" :
            devices.Count == 0 ? "표시 가능한 기기 없음" : $"{devices.Count}개 기기 · 모두 최신 상태";
        _refreshButton.Enabled = true;
        _refreshButton.Text = "↻  새로 고침";
    }

    public void ShowRefreshing()
    {
        _refreshButton.Enabled = false;
        _refreshButton.Text = "새로 고치는 중…";
    }

    private Panel BuildSettingsPanel(AppSettings settings)
    {
        var panel = new Panel { Location = new Point(258, 150), Size = new Size(780, 500), Visible = false };
        panel.Controls.Add(LabelAt("일반", 0, 0, 300, 34, 11, FontStyle.Bold, Theme.Muted));
        _startup = CreateToggle("Windows 시작 시 실행", settings.StartWithWindows, 54);
        _showWidget = CreateToggle("바탕화면 위젯 표시", settings.ShowWidget, 118);
        _lowBattery = CreateToggle("배터리 부족 알림", settings.LowBatteryNotification, 182);
        panel.Controls.AddRange([_startup, _showWidget, _lowBattery]);
        panel.Controls.Add(LabelAt("배터리 부족 기준", 4, 260, 260, 34, 11, FontStyle.Regular, Theme.Text));
        _threshold = new ComboBox
        {
            Location = new Point(570, 256), Size = new Size(160, 36), DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Theme.Surface, ForeColor = Theme.Text, FlatStyle = FlatStyle.Flat
        };
        _threshold.Items.AddRange(["10%", "15%", "20%", "25%", "30%"]);
        _threshold.SelectedItem = $"{settings.LowBatteryThreshold}%";
        if (_threshold.SelectedIndex < 0) _threshold.SelectedItem = "20%";
        panel.Controls.Add(_threshold);
        foreach (var toggle in new[] { _startup, _showWidget, _lowBattery }) toggle.CheckedChanged += SettingsControlChanged;
        _threshold.SelectedIndexChanged += SettingsControlChanged;
        return panel;
    }

    private Panel BuildWidgetPanel()
    {
        var panel = new Panel { Location = new Point(258, 150), Size = new Size(780, 500), Visible = false };
        panel.Controls.Add(LabelAt("바탕화면 위젯", 0, 0, 400, 40, 16, FontStyle.Bold, Theme.Text));
        panel.Controls.Add(LabelAt("연결된 무선 기기의 배터리를 다른 창 아래에서도 빠르게 확인합니다.", 2, 50, 650, 30, 10, FontStyle.Regular, Theme.Muted));
        var preview = new WidgetPreviewControl { Location = new Point(2, 115), Size = new Size(390, 255) };
        panel.Controls.Add(preview);
        panel.Controls.Add(LabelAt("• 위젯을 드래그해 원하는 위치로 이동\n• 우클릭으로 항상 위 표시 또는 숨기기\n• 위치와 표시 상태는 자동 저장", 435, 130, 310, 120, 10, FontStyle.Regular, Theme.Muted));
        return panel;
    }

    private CheckBox CreateToggle(string text, bool value, int top) => new()
    {
        Text = text, Checked = value, Location = new Point(4, top), Size = new Size(726, 42),
        Appearance = Appearance.Button, FlatStyle = FlatStyle.Flat, BackColor = Theme.Surface,
        ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0),
        Font = new Font("Segoe UI", 10.5f)
    };

    private void SettingsControlChanged(object? sender, EventArgs e)
    {
        var threshold = int.TryParse(_threshold.Text.TrimEnd('%'), out var value) ? value : 20;
        SettingsChanged?.Invoke(this, new AppSettings(_showWidget.Checked, _startup.Checked,
            _lowBattery.Checked, threshold));
    }

    private void ShowPage(string page)
    {
        var devices = page == "devices";
        _deviceList.Visible = devices; _refreshButton.Visible = devices;
        _settingsPanel.Visible = page == "settings"; _widgetPanel.Visible = page == "widget";
        _title.Text = devices ? "연결된 기기" : page == "settings" ? "설정" : "위젯";
        _summary.Text = devices ? _summary.Text : page == "settings" ? "Battery Dock 동작을 설정합니다" : "바탕화면 표시 방식을 확인합니다";
        SetNavSelected(_devicesNav, devices); SetNavSelected(_settingsNav, page == "settings"); SetNavSelected(_widgetNav, page == "widget");
    }

    private void Reflow()
    {
        _deviceList.Width = ClientSize.Width - 296;
        _settingsPanel.Width = ClientSize.Width - 296;
        _widgetPanel.Width = ClientSize.Width - 296;
        _refreshButton.Left = ClientSize.Width - 190;
    }

    private static Button CreateNavButton(string text, int top, bool selected)
    {
        var button = new Button
        {
            Text = text, Location = new Point(18, top), Size = new Size(184, 48), FlatStyle = FlatStyle.Flat,
            ForeColor = Theme.Text, BackColor = selected ? Theme.SurfaceHover : Theme.Sidebar,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0),
            Font = new Font("Segoe UI", 11), Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static void SetNavSelected(Button button, bool selected) => button.BackColor = selected ? Theme.SurfaceHover : Theme.Sidebar;

    private static Label LabelAt(string text, int x, int y, int width, int height, float size, FontStyle style, Color color) => new()
    {
        Text = text, Location = new Point(x, y), Size = new Size(width, height),
        Font = new Font("Segoe UI", size, style), ForeColor = color, BackColor = Color.Transparent
    };
}

internal sealed class WidgetPreviewControl : Control
{
    protected override void OnPaint(PaintEventArgs e)
    {
        Theme.EnableQuality(e.Graphics);
        using var path = Theme.RoundedRect(new Rectangle(1, 1, Width - 3, Height - 3), 12);
        e.Graphics.FillPath(new SolidBrush(Theme.Surface), path);
        e.Graphics.DrawPath(new Pen(Theme.Border), path);
        using var title = new Font("Segoe UI", 11, FontStyle.Bold);
        using var row = new Font("Segoe UI", 10);
        e.Graphics.DrawString("Battery Dock", title, new SolidBrush(Theme.Text), 20, 16);
        var entries = new[] { ("MX Keys Mini", 82), ("MX Master 3S", 64), ("WH-1000XM5", 41) };
        for (var i = 0; i < entries.Length; i++)
        {
            var y = 62 + i * 52;
            DeviceListControl.DrawGlyph(e.Graphics, new Rectangle(20, y, 24, 24), (BatteryDeviceKind)i, Theme.Text);
            e.Graphics.DrawString(entries[i].Item1, row, new SolidBrush(Theme.Text), 60, y + 2);
            e.Graphics.DrawString($"{entries[i].Item2}%", row, new SolidBrush(Theme.BatteryColor(entries[i].Item2)), 320, y + 2);
        }
    }
}
