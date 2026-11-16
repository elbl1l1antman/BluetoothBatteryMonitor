namespace BluetoothBatteryMonitor;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var diagnosticsArg = args.FirstOrDefault(a => a.StartsWith("--diagnose=", StringComparison.OrdinalIgnoreCase));
        if (diagnosticsArg is not null)
        {
            DiagnosticsExporter.Export(diagnosticsArg["--diagnose=".Length..].Trim('"'));
            return;
        }
        var snapshotArg = args.FirstOrDefault(a => a.StartsWith("--snapshot=", StringComparison.OrdinalIgnoreCase));
        if (snapshotArg is not null)
        {
            UiSnapshotExporter.Export(snapshotArg["--snapshot=".Length..].Trim('"'));
            return;
        }
        var forceLive = args.Any(a => a.Equals("--live", StringComparison.OrdinalIgnoreCase));
        var demoMode = !forceLive && (args.Any(a => a.Equals("--demo", StringComparison.OrdinalIgnoreCase)) ||
            Application.ExecutablePath.Contains($"{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
        Application.Run(new TrayApplicationContext(demoMode));
    }
}
