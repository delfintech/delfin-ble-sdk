namespace Delfin.Ble.Sdk.Events;

/// <summary>
/// Event arguments emitted when a measurement session completes successfully.
/// </summary>
public class SessionCompletedEventArgs : EventArgs
{
    /// <summary>Unique session identifier.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>The instrument whose session completed. Use <see cref="IInstrument.DeviceSerial"/> for the serial.</summary>
    public required IInstrument Instrument { get; init; }

    /// <summary>UTC timestamp when the session completed.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>All collected measurements keyed by measurement name.</summary>
    public required IReadOnlyDictionary<string, float> Measurements { get; init; }
}
