using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace BluetoothBatteryMonitor;

/// <summary>
/// Reads Windows' public PnP battery-level property. Only devices for which
/// Windows exposes DEVPKEY_Device_BatteryLevel are returned.
/// </summary>
internal sealed class WindowsBatteryDeviceProvider : IBatteryDeviceProvider
{
    private const string Script = """
        $ErrorActionPreference = 'SilentlyContinue'
        $devices = @(Get-PnpDevice -Class Bluetooth -PresentOnly)
        $batteryById = @{}
        if ($devices.Count -gt 0) {
          $properties = @(Get-PnpDeviceProperty -InstanceId @($devices.InstanceId) -KeyName 'DEVPKEY_Device_BatteryLevel')
          foreach ($property in $properties) {
            if ($null -ne $property.Data) {
              $batteryById[$property.InstanceId] = [int]$property.Data
            }
          }
        }
        $items = foreach ($device in $devices) {
          if (-not $batteryById.ContainsKey($device.InstanceId)) { continue }
          $percent = $batteryById[$device.InstanceId]
          if ($percent -lt 0 -or $percent -gt 100) { continue }
          [pscustomobject]@{
            Id = $device.InstanceId
            Name = if ($device.FriendlyName) { $device.FriendlyName } else { $device.InstanceId }
            BatteryPercent = $percent
            IsConnected = ($device.Status -eq 'OK')
            Class = [string]$device.Class
          }
        }
        ConvertTo-Json -InputObject @($items) -Compress
        """;

    public async Task<IReadOnlyList<BatteryDevice>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(Script));
        var start = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -EncodedCommand {encoded}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Windows PowerShell을 시작할 수 없습니다.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            return [];

        try
        {
            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;
            var items = root.ValueKind switch
            {
                JsonValueKind.Array => root.EnumerateArray().ToArray(),
                JsonValueKind.Object => [root],
                _ => []
            };

            var now = DateTime.Now;
            return items
                .Select(item => new BatteryDevice(
                    item.GetProperty("Id").GetString() ?? Guid.NewGuid().ToString(),
                    CleanName(item.GetProperty("Name").GetString()),
                    item.GetProperty("BatteryPercent").GetInt32(),
                    item.TryGetProperty("IsConnected", out var connected) && connected.GetBoolean(),
                    now,
                    GuessKind(item.GetProperty("Name").GetString(), item.TryGetProperty("Class", out var cls) ? cls.GetString() : null)))
                .Where(device => !string.IsNullOrWhiteSpace(device.Name))
                .DistinctBy(device => device.Id)
                .OrderBy(device => device.Name)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string CleanName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "이름 없는 무선 기기" : name.Trim();

    private static BatteryDeviceKind GuessKind(string? name, string? deviceClass)
    {
        var value = $"{name} {deviceClass}".ToLowerInvariant();
        if (value.Contains("keyboard") || value.Contains("키보드") || value.Contains("keys") ||
            value.Contains("durgod") || value.Contains("k310") || value.Contains("k320")) return BatteryDeviceKind.Keyboard;
        if (value.Contains("mouse") || value.Contains("마우스") || value.Contains("master") ||
            value.Contains("keychron m6") || value.Contains("keychron m3")) return BatteryDeviceKind.Mouse;
        if (value.Contains("head") || value.Contains("audio") || value.Contains("이어") || value.Contains("wh-")) return BatteryDeviceKind.Headphones;
        if (value.Contains("game") || value.Contains("controller") || value.Contains("xbox")) return BatteryDeviceKind.Gamepad;
        return BatteryDeviceKind.Other;
    }
}
