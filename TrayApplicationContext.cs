namespace BluetoothBatteryMonitor;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly IBatteryDeviceProvider _provider;
    private readonly DashboardForm _dashboard;
    private readonly DesktopWidgetForm _widget;
    private readonly TrayPopoverForm _popover = new();
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly Dictionary<string, NotifyIcon> _deviceTrayIcons = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Windows.Forms.Timer _timer;
    private IReadOnlyList<BatteryDevice> _devices = [];
    private AppSettings _settings;
    private bool _refreshing;
    private readonly HashSet<string> _notifiedLowDevices = [];

    public TrayApplicationContext(bool demoMode)
    {
        _settings = AppSettings.Load();
        _provider = demoMode ? new DemoBatteryDeviceProvider() : new WindowsBatteryDeviceProvider();
        _dashboard = new DashboardForm(_settings, demoMode);
        _widget = new DesktopWidgetForm(_settings);

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("Battery Dock 열기", null, (_, _) => ShowDashboard());
        var widgetItem = new ToolStripMenuItem("바탕화면 위젯 표시") { CheckOnClick = true, Checked = _settings.ShowWidget };
        widgetItem.CheckedChanged += (_, _) =>
        {
            _settings = _settings with { ShowWidget = widgetItem.Checked };
            if (widgetItem.Checked) _widget.Show(); else _widget.Hide();
            SaveSettings();
        };
        _contextMenu.Items.Add(widgetItem);
        _contextMenu.Items.Add("지금 새로 고침", null, async (_, _) => await RefreshAsync());
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("종료", null, (_, _) => ExitApplication());

        _trayIcon = new NotifyIcon
        {
            Text = "Battery Dock · 확인 중…", Icon = BatteryIconRenderer.CreateControlIcon(), Visible = true, ContextMenuStrip = _contextMenu
        };
        _trayIcon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) _popover.ShowNearTaskbar(_devices); };
        _trayIcon.DoubleClick += (_, _) => ShowDashboard();
        _popover.OpenDashboardRequested += (_, _) => ShowDashboard();
        _dashboard.RefreshRequested += async (_, _) => await RefreshAsync();
        _dashboard.SettingsChanged += (_, updated) =>
        {
            _settings = updated with
            {
                WidgetX = _widget.Left, WidgetY = _widget.Top, WidgetTopMost = _widget.AlwaysOnTop,
                TaskbarDeviceIds = _settings.TaskbarDeviceIds
            };
            try { StartupManager.SetEnabled(_settings.StartWithWindows); } catch { }
            widgetItem.Checked = _settings.ShowWidget;
            SaveSettings();
        };
        _dashboard.TaskbarSelectionChanged += (_, change) =>
        {
            var selected = (_settings.TaskbarDeviceIds ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (change.Selected) selected.Add(change.DeviceId); else selected.Remove(change.DeviceId);
            _settings = _settings with { TaskbarDeviceIds = selected.ToArray() };
            SyncDeviceTrayIcons();
            SaveSettings();
        };
        _widget.HideRequested += (_, _) => widgetItem.Checked = false;
        _widget.StateChanged += (_, _) =>
        {
            _settings = _settings with { WidgetX = _widget.Left, WidgetY = _widget.Top, WidgetTopMost = _widget.AlwaysOnTop };
            SaveSettings();
        };

        _timer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();
        if (_settings.ShowWidget) _widget.Show();
        ShowDashboard();
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (_refreshing) return;
        _refreshing = true;
        _dashboard.ShowRefreshing();
        try
        {
            _devices = await _provider.GetDevicesAsync();
            _dashboard.SetDevices(_devices, _settings.TaskbarDeviceIds);
            _widget.SetDevices(_devices);
            SyncDeviceTrayIcons();
            NotifyLowBatteries();
        }
        catch
        {
            _dashboard.SetDevices(_devices, _settings.TaskbarDeviceIds, isError: true);
        }
        finally { _refreshing = false; }
    }

    private void SyncDeviceTrayIcons()
    {
        var selectedIds = (_settings.TaskbarDeviceIds ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedDevices = _devices.Where(device => selectedIds.Contains(device.Id)).ToArray();
        var visibleIds = selectedDevices.Select(device => device.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var staleId in _deviceTrayIcons.Keys.Where(id => !visibleIds.Contains(id)).ToArray())
        {
            var staleIcon = _deviceTrayIcons[staleId];
            staleIcon.Visible = false;
            staleIcon.Dispose();
            _deviceTrayIcons.Remove(staleId);
        }

        foreach (var device in selectedDevices)
        {
            if (!_deviceTrayIcons.TryGetValue(device.Id, out var notifyIcon))
            {
                notifyIcon = new NotifyIcon { Visible = false, ContextMenuStrip = _contextMenu };
                notifyIcon.MouseClick += (_, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        var currentIds = (_settings.TaskbarDeviceIds ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
                        _popover.ShowNearTaskbar(_devices.Where(d => currentIds.Contains(d.Id)).ToArray());
                    }
                };
                notifyIcon.DoubleClick += (_, _) => ShowDashboard();
                _deviceTrayIcons.Add(device.Id, notifyIcon);
            }

            var previous = notifyIcon.Icon;
            notifyIcon.Icon = BatteryIconRenderer.Create(device.SafeBatteryPercent, device.Kind);
            previous?.Dispose();
            var tooltip = $"{device.Name} · 배터리 {device.SafeBatteryPercent}%";
            notifyIcon.Text = tooltip[..Math.Min(63, tooltip.Length)];
            notifyIcon.Visible = true;
        }

        var controlTooltip = selectedDevices.Length == 0
            ? "Battery Dock · 작업표시줄 표시 기기를 선택하세요"
            : $"Battery Dock · {selectedDevices.Length}개 기기 표시 중";
        _trayIcon.Text = controlTooltip[..Math.Min(63, controlTooltip.Length)];
    }

    private void NotifyLowBatteries()
    {
        if (!_settings.LowBatteryNotification) return;
        foreach (var device in _devices.Where(d => d.SafeBatteryPercent <= _settings.LowBatteryThreshold))
        {
            if (!_notifiedLowDevices.Add(device.Id)) continue;
            _trayIcon.ShowBalloonTip(5000, "배터리가 부족합니다", $"{device.Name} · {device.SafeBatteryPercent}% 남음", ToolTipIcon.Warning);
        }
        _notifiedLowDevices.RemoveWhere(id => _devices.Any(d => d.Id == id && d.SafeBatteryPercent > _settings.LowBatteryThreshold));
    }

    private void ShowDashboard()
    {
        if (!_dashboard.Visible) _dashboard.Show();
        if (_dashboard.WindowState == FormWindowState.Minimized) _dashboard.WindowState = FormWindowState.Normal;
        _dashboard.Activate();
    }

    private void SaveSettings()
    {
        try { _settings.Save(); } catch { }
    }

    private void ExitApplication()
    {
        _timer.Stop();
        foreach (var icon in _deviceTrayIcons.Values)
        {
            icon.Visible = false;
            icon.Dispose();
        }
        _deviceTrayIcons.Clear();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _popover.Dispose();
        _widget.Dispose();
        _dashboard.Dispose();
        _contextMenu.Dispose();
        ExitThread();
    }
}
