namespace BluetoothBatteryMonitor;

internal enum BatteryDeviceKind { Keyboard, Mouse, Headphones, Gamepad, Other }

internal sealed record BatteryDevice(
    string Id,
    string Name,
    int BatteryPercent,
    bool IsConnected,
    DateTime UpdatedAt,
    BatteryDeviceKind Kind)
{
    public int SafeBatteryPercent => Math.Clamp(BatteryPercent, 0, 100);
}
