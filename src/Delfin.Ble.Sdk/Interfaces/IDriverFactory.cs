namespace Delfin.Ble.Sdk;

/// <summary>
/// Factory for creating Driver instances. Loaded at runtime from the Driver DLL.
/// </summary>
public interface IDriverFactory
{
    /// <summary>
    /// Detects the BleuIO dongle COM port. Run once, then pass the result as SerialPortName.
    /// </summary>
    /// <returns>COM port name (e.g. "COM4"), or null if no dongle found.</returns>
    Task<string?> FindPortAsync(CancellationToken ct = default);

    IDeviceClient CreateClient(DriverOptions options);
}

public record DriverOptions(
    string ConfigFilePath,
    string? SerialPortName = null,
    TimeSpan? ScanTimeout = null,
    TimeSpan? ConnectionTimeout = null,
    TimeSpan? ReconnectionTimeout = null,
    /// <summary>Device serials to auto-connect on initialization. Empty = connect to any found device.</summary>
    IReadOnlyList<string>? DeviceSerials = null,
    /// <summary>RuuviTag MAC addresses to monitor. Null/empty = accept all sensors.</summary>
    IReadOnlyList<string>? SensorMacs = null,
    /// <summary>Rate limit interval for sensor data delivery. Default: 10 seconds.</summary>
    TimeSpan? SensorRateLimitInterval = null,
    /// <summary>When true, all raw protocol values are included in measurement results (bypasses api_parameters filter).</summary>
    bool RawMode = false
);
