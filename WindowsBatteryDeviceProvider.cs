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
        $items = foreach ($device in Get-PnpDevice -PresentOnly) {
          $battery = Get-PnpDeviceProperty -InstanceId $device.InstanceId -KeyName 'DEVPKEY_Device_BatteryLevel'
          if ($null -eq $battery -or $null -eq $battery.Data) { continue }
          $percent = [int]$battery.Data
          if ($percent -lt 0 -or $percent -gt 100) { continue }
          [pscustomobject]@{
            Id = $device.InstanceId
            Name = if ($device.FriendlyName) { $device.FriendlyName } else { $device.InstanceId }
            BatteryPercent = $percent
            IsConnected = ($device.Status -eq 'OK')
            Class = [string]$device.Class
          }
        }
        @($items) | ConvertTo-Json -Compress
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
            if (root.ValueKind != JsonValueKind.Array)
                return [];

            var now = DateTime.Now;
            return root.EnumerateArray()
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
        if (value.Contains("keyboard") || value.Contains("키보드") || value.Contains("keys")) return BatteryDeviceKind.Keyboard;
        if (value.Contains("mouse") || value.Contains("마우스") || value.Contains("master")) return BatteryDeviceKind.Mouse;
        if (value.Contains("head") || value.Contains("audio") || value.Contains("이어") || value.Contains("wh-")) return BatteryDeviceKind.Headphones;
        if (value.Contains("game") || value.Contains("controller") || value.Contains("xbox")) return BatteryDeviceKind.Gamepad;
        return BatteryDeviceKind.Other;
    }
}
