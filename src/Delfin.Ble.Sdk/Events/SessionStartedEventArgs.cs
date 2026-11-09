namespace Delfin.Ble.Sdk.Events;

/// <summary>
/// Event arguments emitted when a measurement session starts.
/// </summary>
public class SessionStartedEventArgs : EventArgs
{
    /// <summary>Unique session identifier (UUID v4).</summary>
    public required Guid SessionId { get; init; }

    /// <summary>The instrument that started the session. Use <see cref="IInstrument.DeviceSerial"/> for the serial.</summary>
    public required IInstrument Instrument { get; init; }

    /// <summary>UTC timestamp when the session started.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Active measurement mode/probe, if applicable (e.g., "sda", "nai", "L50").</summary>
    public string? Mode { get; init; }
}
