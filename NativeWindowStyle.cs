using System.Runtime.InteropServices;

namespace BluetoothBatteryMonitor;

internal static class NativeWindowStyle
{
    public static void ApplyDark(Form form)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10)) return;
        var enabled = 1;
        var caption = ColorTranslator.ToWin32(Theme.Sidebar);
        var border = ColorTranslator.ToWin32(Theme.Border);
        var text = ColorTranslator.ToWin32(Theme.Text);
        DwmSetWindowAttribute(form.Handle, 20, ref enabled, sizeof(int));
        DwmSetWindowAttribute(form.Handle, 35, ref caption, sizeof(int));
        DwmSetWindowAttribute(form.Handle, 34, ref border, sizeof(int));
        DwmSetWindowAttribute(form.Handle, 36, ref text, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
