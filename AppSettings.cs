using System.Text.Json;

namespace BluetoothBatteryMonitor;

internal sealed record AppSettings(
    bool ShowWidget = true,
    bool StartWithWindows = false,
    bool LowBatteryNotification = true,
    int LowBatteryThreshold = 20,
    int WidgetX = int.MinValue,
    int WidgetY = int.MinValue,
    bool WidgetTopMost = false,
    string[]? TaskbarDeviceIds = null)
{
    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BatteryDock", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            return File.Exists(SettingsPath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new()
                : new();
        }
        catch { return new(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
