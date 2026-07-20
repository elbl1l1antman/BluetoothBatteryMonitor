namespace BluetoothBatteryMonitor;

internal sealed class DemoBatteryDeviceProvider : IBatteryDeviceProvider
{
    public Task<IReadOnlyList<BatteryDevice>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        IReadOnlyList<BatteryDevice> devices =
        [
            new("demo-keyboard", "MX Keys Mini", 82, true, now, BatteryDeviceKind.Keyboard),
            new("demo-mouse", "MX Master 3S", 64, true, now, BatteryDeviceKind.Mouse),
            new("demo-headphones", "WH-1000XM5", 41, true, now, BatteryDeviceKind.Headphones),
            new("demo-gamepad", "Xbox Wireless Controller", 18, true, now, BatteryDeviceKind.Gamepad)
        ];
        return Task.FromResult(devices);
    }
}
