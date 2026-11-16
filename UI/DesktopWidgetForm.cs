using System.Runtime.InteropServices;

namespace BluetoothBatteryMonitor;

internal sealed class DesktopWidgetForm : Form
{
    private IReadOnlyList<BatteryDevice> _devices = [];
    private bool _dragging;
    private Point _dragOffset;
    private readonly ToolStripMenuItem _topMostItem;

    public event EventHandler? HideRequested;
    public event EventHandler? StateChanged;

    public DesktopWidgetForm(AppSettings settings)
    {
        Text = "Battery Dock 위젯";
        ClientSize = new Size(365, 244);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Theme.Surface;
        Opacity = 0.97;
        TopMost = settings.WidgetTopMost;
        DoubleBuffered = true;

        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        Location = settings.WidgetX == int.MinValue
            ? new Point(area.Right - Width - 28, area.Bottom - Height - 28)
            : new Point(Math.Clamp(settings.WidgetX, area.Left, area.Right - Width), Math.Clamp(settings.WidgetY, area.Top, area.Bottom - Height));

        _topMostItem = new ToolStripMenuItem("항상 위에 표시") { CheckOnClick = true, Checked = settings.WidgetTopMost };
        _topMostItem.CheckedChanged += (_, _) => { TopMost = _topMostItem.Checked; StateChanged?.Invoke(this, EventArgs.Empty); };
        var menu = new ContextMenuStrip();
        menu.Items.Add(_topMostItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("위젯 숨기기", null, (_, _) => HideRequested?.Invoke(this, EventArgs.Empty));
        ContextMenuStrip = menu;

        MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { _dragging = true; _dragOffset = e.Location; } };
        MouseMove += (_, e) => { if (_dragging) Location = new Point(Cursor.Position.X - _dragOffset.X, Cursor.Position.Y - _dragOffset.Y); };
        MouseUp += (_, _) => { _dragging = false; StateChanged?.Invoke(this, EventArgs.Empty); };
        FormClosing += (_, e) => { if (e.CloseReason != CloseReason.ApplicationExitCall) { e.Cancel = true; HideRequested?.Invoke(this, EventArgs.Empty); } };
        SizeChanged += (_, _) => UpdateRegion();
        UpdateRegion();
    }

    public bool AlwaysOnTop => _topMostItem.Checked;

    public void SetDevices(IReadOnlyList<BatteryDevice> devices)
    {
        _devices = devices.Take(4).ToArray();
        Height = Math.Max(150, 76 + _devices.Count * 45);
        Invalidate();
        UpdateRegion();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Theme.EnableQuality(e.Graphics);
        e.Graphics.Clear(Theme.Surface);
        using var border = Theme.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 12);
        e.Graphics.DrawPath(new Pen(Theme.Border), border);
        using var titleFont = new Font("Segoe UI", 11.5f, FontStyle.Bold);
        using var rowFont = new Font("Segoe UI", 10.5f);
        using var mutedFont = new Font("Segoe UI", 8.5f);
        e.Graphics.DrawString("Battery Dock", titleFont, new SolidBrush(Theme.Text), 18, 15);
        e.Graphics.DrawString("⋮", titleFont, new SolidBrush(Theme.Muted), Width - 34, 13);

        if (_devices.Count == 0)
        {
            e.Graphics.DrawString("표시 가능한 기기 없음", rowFont, new SolidBrush(Theme.Muted), 18, 72);
            return;
        }

        for (var i = 0; i < _devices.Count; i++)
        {
            var device = _devices[i];
            var y = 62 + i * 45;
            DeviceListControl.DrawGlyph(e.Graphics, new Rectangle(18, y, 23, 23), device.Kind, Theme.Text);
            e.Graphics.DrawString(device.Name, rowFont, new SolidBrush(Theme.Text), 55, y + 1);
            using var format = new StringFormat { Alignment = StringAlignment.Far };
            e.Graphics.DrawString($"{device.SafeBatteryPercent}%", rowFont, new SolidBrush(Theme.BatteryColor(device.SafeBatteryPercent)),
                new RectangleF(270, y + 1, 74, 24), format);
        }
        e.Graphics.DrawString($"업데이트: {DateTime.Now:tt h:mm}", mutedFont, new SolidBrush(Theme.Muted), 18, Height - 27);
    }

    private void UpdateRegion()
    {
        using var path = Theme.RoundedRect(new Rectangle(0, 0, Width, Height), 12);
        Region?.Dispose();
        Region = new Region(path);
    }
}
