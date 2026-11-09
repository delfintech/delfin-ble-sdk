namespace Delfin.Ble.Sdk.Events;

/// <summary>
/// Event args raised when an instrument connects (first time or reconnection).
/// </summary>
public class InstrumentConnectedEventArgs : EventArgs
{
    /// <summary>The instrument that connected. Use <see cref="IInstrument.DeviceSerial"/> for the serial.</summary>
    public required IInstrument Instrument { get; init; }

    /// <summary>True when this is a reconnection of a previously-connected instrument.</summary>
    public bool IsReconnection { get; init; }
}
