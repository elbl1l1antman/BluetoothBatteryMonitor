using System.Text.Json;

namespace BluetoothBatteryMonitor;

internal static class DiagnosticsExporter
{
    public static void Export(string outputPath)
    {
        var startedAt = DateTime.Now;
        var devices = new WindowsBatteryDeviceProvider().GetDevicesAsync().GetAwaiter().GetResult();
        var result = new
        {
            StartedAt = startedAt,
            FinishedAt = DateTime.Now,
            IsAdministrator = IsAdministrator(),
            DeviceCount = devices.Count,
            Devices = devices
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        File.WriteAllText(outputPath, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static bool IsAdministrator()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
}
