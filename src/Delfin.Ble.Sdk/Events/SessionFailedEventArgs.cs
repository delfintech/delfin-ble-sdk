namespace Delfin.Ble.Sdk.Events;

/// <summary>
/// Event arguments emitted when a measurement session fails.
/// </summary>
public class SessionFailedEventArgs : EventArgs
{
    /// <summary>Unique session identifier.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>The instrument whose session failed. Use <see cref="IInstrument.DeviceSerial"/> for the serial.</summary>
    public required IInstrument Instrument { get; init; }

    /// <summary>UTC timestamp when the session failed.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Error context describing the failure, or null if not available.</summary>
    public string? ErrorContext { get; init; }
}
