using System.Drawing.Imaging;

namespace BluetoothBatteryMonitor;

internal static class UiSnapshotExporter
{
    public static void Export(string outputPath)
    {
        var selectedIds = new[] { "demo-keyboard", "demo-mouse" };
        using var form = new DashboardForm(new AppSettings(TaskbarDeviceIds: selectedIds), demoMode: true)
        {
            FormBorderStyle = FormBorderStyle.None,
            ClientSize = new Size(1080, 700),
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-2000, -2000)
        };
        var devices = new DemoBatteryDeviceProvider().GetDevicesAsync().GetAwaiter().GetResult();
        form.SetDevices(devices, selectedIds);
        form.Show();
        Application.DoEvents();
        using var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        bitmap.Save(outputPath, ImageFormat.Png);
        form.Close();
    }
}
