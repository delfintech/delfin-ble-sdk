using Delfin.Ble.Sdk;
using Delfin.Ble.Sdk.Models;

namespace Delfin.Ble.Sdk.Events;

/// <summary>
/// Event arguments for a measurement value received during an active session.
/// </summary>
public class MeasurementReceivedEventArgs : EventArgs
{
    /// <summary>Measurement name (unit_name from device configuration).</summary>
    public required string MeasurementName { get; init; }

    /// <summary>Raw numeric value from the device (unrounded).</summary>
    public required float RawValue { get; init; }

    /// <summary>Unit string identifier from device configuration.</summary>
    public required string UnitString { get; init; }

    /// <summary>Display formatting hint with decimal places and unit.</summary>
    public required DisplayHint DisplayHint { get; init; }

    /// <summary>Session ID this measurement belongs to.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>The instrument that produced this measurement. Use <see cref="IInstrument.DeviceSerial"/> for the serial.</summary>
    public required IInstrument Instrument { get; init; }

    /// <summary>Timestamp when the session started.</summary>
    public required DateTimeOffset SessionStartTime { get; init; }
}
