using System.Drawing.Drawing2D;

namespace BluetoothBatteryMonitor;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(12, 20, 29);
    public static readonly Color Sidebar = Color.FromArgb(17, 27, 38);
    public static readonly Color Surface = Color.FromArgb(22, 33, 45);
    public static readonly Color SurfaceHover = Color.FromArgb(31, 44, 57);
    public static readonly Color Border = Color.FromArgb(58, 71, 84);
    public static readonly Color Text = Color.FromArgb(245, 247, 250);
    public static readonly Color Muted = Color.FromArgb(160, 171, 184);
    public static readonly Color Green = Color.FromArgb(109, 231, 143);
    public static readonly Color Amber = Color.FromArgb(255, 193, 59);
    public static readonly Color Red = Color.FromArgb(255, 88, 96);
    public static readonly Color Track = Color.FromArgb(61, 73, 84);

    public static Color BatteryColor(int percent) => percent <= 20 ? Red : percent <= 50 ? Amber : Green;

    public static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void EnableQuality(Graphics graphics)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
    }
}
