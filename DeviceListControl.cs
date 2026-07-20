using System.Drawing.Drawing2D;

namespace BluetoothBatteryMonitor;

internal sealed class DeviceListControl : Control
{
    private IReadOnlyList<BatteryDevice> _devices = [];
    private HashSet<string> _taskbarDeviceIds = [];
    private bool _isLoading = true;

    public event EventHandler<(string DeviceId, bool Selected)>? TaskbarSelectionChanged;

    public DeviceListControl()
    {
        DoubleBuffered = true;
        BackColor = Theme.Background;
    }

    public void SetDevices(IReadOnlyList<BatteryDevice> devices, IEnumerable<string>? taskbarDeviceIds = null)
    {
        _devices = devices;
        _isLoading = false;
        if (taskbarDeviceIds is not null)
            _taskbarDeviceIds = taskbarDeviceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left || e.X < Width - 55 || e.Y < 41) return;
        var row = (e.Y - 41) / 98;
        if (row < 0 || row >= _devices.Count) return;
        var id = _devices[row].Id;
        var selected = !_taskbarDeviceIds.Contains(id);
        if (selected) _taskbarDeviceIds.Add(id); else _taskbarDeviceIds.Remove(id);
        Invalidate();
        TaskbarSelectionChanged?.Invoke(this, (id, selected));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Theme.EnableQuality(e.Graphics);
        if (_devices.Count == 0)
        {
            DrawEmptyState(e.Graphics);
            return;
        }

        using var headerFont = new Font("Segoe UI", 9.5f);
        using var rowFont = new Font("Segoe UI", 11.5f);
        using var stateFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        using var percentFont = new Font("Segoe UI", 15f, FontStyle.Bold);
        using var muted = new SolidBrush(Theme.Muted);
        using var text = new SolidBrush(Theme.Text);
        using var green = new SolidBrush(Theme.Green);
        using var line = new Pen(Color.FromArgb(45, 58, 70));

        e.Graphics.DrawString("기기", headerFont, muted, 8, 10);
        var stateX = (int)(Width * .40f);
        var trackX = (int)(Width * .55f);
        var trackWidth = Math.Clamp((int)(Width * .155f), 105, 145);
        var percentX = trackX + trackWidth + 18;
        var updateX = Width - 145;
        e.Graphics.DrawString("상태", headerFont, muted, stateX, 10);
        e.Graphics.DrawString("배터리", headerFont, muted, trackX, 10);
        e.Graphics.DrawString("업데이트", headerFont, muted, updateX, 10);
        e.Graphics.DrawString("표시", headerFont, muted, Width - 43, 10);
        e.Graphics.DrawLine(line, 8, 40, Width - 8, 40);

        for (var i = 0; i < _devices.Count; i++)
        {
            var device = _devices[i];
            var top = 41 + i * 98;
            DrawGlyph(e.Graphics, new Rectangle(13, top + 27, 32, 32), device.Kind, Theme.Text);
            e.Graphics.DrawString(device.Name, rowFont, text, 62, top + 31);
            e.Graphics.DrawString(device.IsConnected ? "연결됨" : "연결 끊김", stateFont,
                device.IsConnected ? green : muted, stateX, top + 33);

            var trackRect = new Rectangle(trackX, top + 38, trackWidth, 10);
            using (var trackPath = Theme.RoundedRect(trackRect, 5))
                e.Graphics.FillPath(new SolidBrush(Theme.Track), trackPath);
            var fillRect = new Rectangle(trackRect.X, trackRect.Y,
                Math.Max(8, (int)(trackRect.Width * device.SafeBatteryPercent / 100f)), trackRect.Height);
            using (var fillPath = Theme.RoundedRect(fillRect, 5))
            using (var fill = new SolidBrush(Theme.BatteryColor(device.SafeBatteryPercent)))
                e.Graphics.FillPath(fill, fillPath);
            e.Graphics.DrawString($"{device.SafeBatteryPercent}%", percentFont, text, percentX, top + 26);
            var updated = device.UpdatedAt.ToString("tt h:mm");
            e.Graphics.DrawString(updated, rowFont, muted, updateX, top + 31);
            DrawTaskbarCheck(e.Graphics, new Rectangle(Width - 42, top + 31, 22, 22), _taskbarDeviceIds.Contains(device.Id));
            e.Graphics.DrawLine(line, 8, top + 97, Width - 8, top + 97);
        }
    }

    private static void DrawTaskbarCheck(Graphics graphics, Rectangle rect, bool selected)
    {
        using var path = Theme.RoundedRect(rect, 5);
        using var background = new SolidBrush(selected ? Theme.Green : Theme.Surface);
        using var border = new Pen(selected ? Theme.Green : Theme.Border, 1.4f);
        graphics.FillPath(background, path);
        graphics.DrawPath(border, path);
        if (!selected) return;
        using var check = new Pen(Theme.Background, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        graphics.DrawLines(check,
        [
            new Point(rect.X + 5, rect.Y + 11),
            new Point(rect.X + 9, rect.Y + 15),
            new Point(rect.X + 17, rect.Y + 7)
        ]);
    }

    private void DrawEmptyState(Graphics graphics)
    {
        var centerX = Width / 2;
        DrawGlyph(graphics, new Rectangle(centerX - 24, 105, 48, 48), BatteryDeviceKind.Other, Theme.Muted);
        using var title = new Font("Segoe UI", 14, FontStyle.Bold);
        using var body = new Font("Segoe UI", 10);
        using var text = new SolidBrush(Theme.Text);
        using var muted = new SolidBrush(Theme.Muted);
        using var centered = new StringFormat { Alignment = StringAlignment.Center };
        graphics.DrawString(_isLoading ? "배터리 정보를 확인하고 있습니다" : "배터리 정보를 제공하는 기기가 없습니다", title, text,
            new RectangleF(0, 174, Width, 26), centered);
        graphics.DrawString(_isLoading ? "연결된 Bluetooth 기기를 검색하는 중입니다." :
            "Bluetooth 기기를 연결한 뒤 새로 고침해 주세요.\nWindows에 배터리 잔량이 표시되는 기기만 나타납니다.",
            body, muted, new RectangleF(0, 212, Width, 55), centered);
    }

    internal static void DrawGlyph(Graphics g, Rectangle rect, BatteryDeviceKind kind, Color color)
    {
        using var pen = new Pen(color, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        var x = rect.X; var y = rect.Y; var w = rect.Width; var h = rect.Height;
        switch (kind)
        {
            case BatteryDeviceKind.Keyboard:
                g.DrawRoundedRectangle(pen, new Rectangle(x + 1, y + 6, w - 2, h - 12), 4);
                for (var row = 0; row < 3; row++)
                    for (var col = 0; col < 6; col++)
                        g.FillRectangle(new SolidBrush(color), x + 5 + col * 4, y + 11 + row * 4, 2, 2);
                break;
            case BatteryDeviceKind.Mouse:
                g.DrawEllipse(pen, x + 6, y + 1, w - 12, h - 2);
                g.DrawLine(pen, x + w / 2, y + 2, x + w / 2, y + 11);
                break;
            case BatteryDeviceKind.Headphones:
                g.DrawArc(pen, x + 3, y + 3, w - 6, h - 4, 190, 160);
                g.DrawRoundedRectangle(pen, new Rectangle(x + 2, y + 16, 7, 13), 3);
                g.DrawRoundedRectangle(pen, new Rectangle(x + w - 9, y + 16, 7, 13), 3);
                break;
            case BatteryDeviceKind.Gamepad:
                g.DrawArc(pen, x + 2, y + 6, w - 4, h - 8, 175, 190);
                g.DrawLine(pen, x + 3, y + 19, x + 1, y + 28);
                g.DrawLine(pen, x + w - 3, y + 19, x + w - 1, y + 28);
                g.DrawLine(pen, x + 9, y + 14, x + 15, y + 14);
                g.DrawLine(pen, x + 12, y + 11, x + 12, y + 17);
                g.FillEllipse(new SolidBrush(color), x + 22, y + 11, 3, 3);
                g.FillEllipse(new SolidBrush(color), x + 26, y + 15, 3, 3);
                break;
            default:
                g.DrawRoundedRectangle(pen, new Rectangle(x + 2, y + 7, w - 7, h - 14), 4);
                g.DrawLine(pen, x + w - 5, y + 13, x + w - 2, y + 13);
                g.DrawLine(pen, x + w - 2, y + 13, x + w - 2, y + h - 13);
                g.DrawLine(pen, x + w - 2, y + h - 13, x + w - 5, y + h - 13);
                break;
        }
    }
}

internal static class DrawingExtensions
{
    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle rectangle, int radius)
    {
        using var path = Theme.RoundedRect(rectangle, radius);
        graphics.DrawPath(pen, path);
    }
}
