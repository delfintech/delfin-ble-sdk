using Delfin.Ble.Sdk.Events;
using Delfin.Ble.Sdk.Models;

namespace Delfin.Ble.Sdk;

/// <summary>
/// Represents a connected Delfin measurement device.
/// </summary>
public interface IInstrument
{
    string DeviceSerial { get; }
    ConnectionState State { get; }
    BatteryLevel? BatteryStatus { get; }
    float? BatteryVoltage { get; }

    /// <summary>
    /// Active mode/probe the device reports, or null if the device has no mode concept.
    /// On MoistureMeterD (D3N) this is the probe (XS5/S15/M25/L50); on VapoMeter SWL it is
    /// the measurement mode (nai/sda/eye).
    /// </summary>
    string? Mode { get; }

    /// <summary>
    /// Interchangeable sensor identifier (hex) reported by DMI2 devices (e.g. VapoMeter),
    /// or null if not yet received. This is the physical sensor head ID, distinct from
    /// <see cref="Mode"/> (which is the D3N probe selection).
    /// </summary>
    string? SensorId { get; }

    /// <summary>
    /// Device-lifetime measurement counter reported by the instrument, or null if not yet
    /// received. Tracked at the instrument level (not per measurement) because the count
    /// message can arrive before or after a session terminator.
    /// </summary>
    int? MeasurementCount { get; }

    (int Year, int Month)? CalibrationValidUntil { get; }
    Guid? ActiveSessionId { get; }

    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);

    // Session & measurement events
    event EventHandler<SessionStartedEventArgs>? SessionStarted;
    event EventHandler<SessionCompletedEventArgs>? SessionCompleted;
    event EventHandler<SessionCancelledEventArgs>? SessionCancelled;
    event EventHandler<SessionFailedEventArgs>? SessionFailed;
    event EventHandler<MeasurementReceivedEventArgs>? MeasurementReceived;

    /// <summary>
    /// Fired when a measurement session completes (Option A: all data bundled).
    /// Contains the full measurement result with all collected values.
    /// </summary>
    event EventHandler<MeasurementResult>? MeasurementCompleted;

    // Device status events
    event EventHandler<BatteryEventArgs>? BatteryLow;
    event EventHandler<BatteryEventArgs>? BatteryEmpty;
    event EventHandler<CalibrationDueEventArgs>? CalibrationDue;

    // Connection events
    event EventHandler<ConnectionEventArgs>? ConnectionLost;
    event EventHandler<ConnectionEventArgs>? Reconnected;
    event EventHandler<ConnectionEventArgs>? ReconnectionFailed;

    // Error events
    event EventHandler<ErrorOccurredEventArgs>? ErrorOccurred;
}
