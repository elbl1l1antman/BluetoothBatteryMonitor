using System.Drawing.Drawing2D;

namespace BluetoothBatteryMonitor;

internal static class BatteryIconRenderer
{
    public static Icon Create(int percent, BatteryDeviceKind kind = BatteryDeviceKind.Other)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        var color = Theme.BatteryColor(percent);
        using var outline = new Pen(Color.White, 2.2f);
        using var fill = new SolidBrush(color);
        using var track = new SolidBrush(Color.FromArgb(72, 82, 94));
        FillRoundedRectangle(graphics, track, new Rectangle(3, 9, 24, 14), 3);
        DrawRoundedRectangle(graphics, outline, new Rectangle(3, 9, 24, 14), 3);
        graphics.FillRectangle(fill, 6, 12, (int)(18 * Math.Clamp(percent, 0, 100) / 100f), 8);
        graphics.FillRectangle(Brushes.White, 28, 13, 2, 6);
        using var labelFont = new Font("Segoe UI", 7.2f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var labelBrush = new SolidBrush(Color.White);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString(KindLabel(kind), labelFont, labelBrush, new RectangleF(4, 9, 22, 14), format);
        return Icon.FromHandle(bitmap.GetHicon());
    }

    public static Icon CreateControlIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        using var surface = new SolidBrush(Theme.Surface);
        using var outline = new Pen(Color.White, 2f);
        using var accent = new SolidBrush(Theme.Green);
        FillRoundedRectangle(graphics, surface, new Rectangle(3, 7, 24, 18), 4);
        DrawRoundedRectangle(graphics, outline, new Rectangle(3, 7, 24, 18), 4);
        graphics.FillRectangle(accent, 7, 11, 15, 10);
        graphics.FillRectangle(Brushes.White, 28, 12, 2, 8);
        return Icon.FromHandle(bitmap.GetHicon());
    }

    private static string KindLabel(BatteryDeviceKind kind) => kind switch
    {
        BatteryDeviceKind.Keyboard => "K",
        BatteryDeviceKind.Mouse => "M",
        BatteryDeviceKind.Headphones => "H",
        BatteryDeviceKind.Gamepad => "G",
        _ => "•"
    };

    private static void FillRoundedRectangle(Graphics g, Brush brush, Rectangle rect, int radius)
    {
        using var path = Theme.RoundedRect(rect, radius);
        g.FillPath(brush, path);
    }

    private static void DrawRoundedRectangle(Graphics g, Pen pen, Rectangle rect, int radius)
    {
        using var path = Theme.RoundedRect(rect, radius);
        g.DrawPath(pen, path);
    }
}
