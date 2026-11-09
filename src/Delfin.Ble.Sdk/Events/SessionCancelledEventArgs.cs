namespace Delfin.Ble.Sdk.Events;

/// <summary>
/// Event arguments emitted when a measurement session is cancelled.
/// </summary>
public class SessionCancelledEventArgs : EventArgs
{
    /// <summary>Unique session identifier.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>The instrument whose session was cancelled. Use <see cref="IInstrument.DeviceSerial"/> for the serial.</summary>
    public required IInstrument Instrument { get; init; }

    /// <summary>UTC timestamp when the session was cancelled.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Reason for cancellation (e.g., "superseded"), or null if not specified.</summary>
    public string? Reason { get; init; }
}
