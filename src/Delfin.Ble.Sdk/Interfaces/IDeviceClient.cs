using Delfin.Ble.Sdk.Events;
using Delfin.Ble.Sdk.Models;

namespace Delfin.Ble.Sdk;

/// <summary>
/// Top-level client managing dongle lifecycle and device discovery.
/// </summary>
public interface IDeviceClient : IDisposable
{
    /// <summary>
    /// Version of the loaded Driver, derived from the git tag it was built from
    /// (e.g. "1.1.1-RC1+abc1234"). Returns "unknown" if the version cannot be determined.
    /// Use this for logging, support diagnostics, and traceability to a specific build.
    /// </summary>
    string DriverVersion { get; }

    /// <summary>Current dongle connection state.</summary>
    bool IsDongleConnected { get; }

    /// <summary>All known instrument instances keyed by device serial.</summary>
    IReadOnlyDictionary<string, IInstrument> Instruments { get; }

    /// <summary>Scan for nearby Delfin BLE devices.</summary>
    Task<IReadOnlyList<DeviceInfo>> ScanAsync(
        string? serialFilter = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    /// <summary>Connect to a device by address.</summary>
    Task<IInstrument> ConnectAsync(string deviceAddress, CancellationToken ct = default);

    /// <summary>
    /// Initialize the client: detect dongle, open serial port, configure BLE transport.
    /// Must be called before ScanAsync or ConnectAsync.
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>Provides access to the room sensor scanner for background BLE scanning.</summary>
    IRoomSensorScanner RoomSensorScanner { get; }

    /// <summary>Disconnect all devices and release resources.</summary>
    Task DisconnectAllAsync(CancellationToken ct = default);

    // Global events (dongle-level)
    event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;
    event EventHandler? DongleDetected;
    event EventHandler? DongleLost;
    event EventHandler? DongleRecovered;

    /// <summary>
    /// Raised when an instrument connects or reconnects.
    /// Use this to subscribe to instrument events for devices that connect asynchronously.
    /// </summary>
    event EventHandler<InstrumentConnectedEventArgs>? InstrumentConnected;
}
