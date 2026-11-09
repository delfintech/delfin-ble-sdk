using Delfin.Ble.Sdk.Events;
using Delfin.Ble.Sdk.Models;

namespace Delfin.Ble.Sdk;

/// <summary>
/// Manages background scanning for room environment sensors.
/// Provides unified data from Delfin room sensors and RuuviTags.
/// </summary>
public interface IRoomSensorScanner : IAsyncDisposable
{
    /// <summary>Whether scanning is currently active.</summary>
    bool IsScanning { get; }

    /// <summary>Currently configured MAC/ID filter. Empty = accept all.</summary>
    IReadOnlySet<string> MacFilter { get; }

    /// <summary>Start continuous background scanning.</summary>
    Task StartScanningAsync(RoomSensorScanOptions? options = null, CancellationToken ct = default);

    /// <summary>Stop background scanning.</summary>
    Task StopScanningAsync(CancellationToken ct = default);

    /// <summary>Update the MAC filter while scanning is active or stopped.</summary>
    void SetMacFilter(IEnumerable<string> allowedMacs);

    /// <summary>Clear the MAC filter (accept all sensors).</summary>
    void ClearMacFilter();

    /// <summary>Raised when a new room sensor reading passes filtering and rate limiting.</summary>
    event EventHandler<RoomSensorReadingEventArgs>? SensorDataReceived;

    /// <summary>Raised when a previously unseen sensor is discovered during a scanning session.</summary>
    event EventHandler<SensorDiscoveredEventArgs>? SensorDiscovered;

    /// <summary>Raised when a scanning error occurs (dongle unresponsive or disconnected).</summary>
    event EventHandler<ScanErrorEventArgs>? ScanError;
}
