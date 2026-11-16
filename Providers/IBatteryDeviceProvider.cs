namespace BluetoothBatteryMonitor;

internal interface IBatteryDeviceProvider
{
    Task<IReadOnlyList<BatteryDevice>> GetDevicesAsync(CancellationToken cancellationToken = default);
}
