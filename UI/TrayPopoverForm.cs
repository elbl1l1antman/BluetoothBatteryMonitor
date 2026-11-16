namespace BluetoothBatteryMonitor;

internal sealed class TrayPopoverForm : Form
{
    private IReadOnlyList<BatteryDevice> _devices = [];
    public event EventHandler? OpenDashboardRequested;

    public TrayPopoverForm()
    {
        ClientSize = new Size(350, 290);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Theme.Surface;
        TopMost = true;
        DoubleBuffered = true;
        Deactivate += (_, _) => Hide();
        MouseClick += (_, e) =>
        {
            if (e.Y >= Height - 48) { Hide(); OpenDashboardRequested?.Invoke(this, EventArgs.Empty); }
        };
    }

    public void ShowNearTaskbar(IReadOnlyList<BatteryDevice> devices)
    {
        _devices = devices.Take(4).ToArray();
        Height = 94 + Math.Max(1, _devices.Count) * 45;
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(Math.Min(Cursor.Position.X - Width + 18, area.Right - Width - 10), area.Bottom - Height - 10);
        Invalidate();
        Show();
        Activate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Theme.EnableQuality(e.Graphics);
        e.Graphics.Clear(Theme.Surface);
        using var border = Theme.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 10);
        e.Graphics.DrawPath(new Pen(Theme.Border), border);
        using var title = new Font("Segoe UI", 11, FontStyle.Bold);
        using var row = new Font("Segoe UI", 10);
        e.Graphics.DrawString("Battery Dock", title, new SolidBrush(Theme.Text), 18, 14);
        if (_devices.Count == 0)
            e.Graphics.DrawString("표시 가능한 기기 없음", row, new SolidBrush(Theme.Muted), 18, 55);
        else
            for (var i = 0; i < _devices.Count; i++)
            {
                var device = _devices[i];
                var y = 53 + i * 45;
                DeviceListControl.DrawGlyph(e.Graphics, new Rectangle(18, y, 22, 22), device.Kind, Theme.Text);
                e.Graphics.DrawString(device.Name, row, new SolidBrush(Theme.Text), 54, y);
                using var format = new StringFormat { Alignment = StringAlignment.Far };
                e.Graphics.DrawString($"{device.SafeBatteryPercent}%", row, new SolidBrush(Theme.BatteryColor(device.SafeBatteryPercent)),
                    new RectangleF(270, y, 60, 25), format);
            }
        e.Graphics.DrawLine(new Pen(Theme.Border), 0, Height - 48, Width, Height - 48);
        e.Graphics.DrawString("↗   Battery Dock 열기", row, new SolidBrush(Theme.Text), 18, Height - 34);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = Theme.RoundedRect(new Rectangle(0, 0, Width, Height), 10);
        Region?.Dispose();
        Region = new Region(path);
    }
}
