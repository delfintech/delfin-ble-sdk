using Delfin.Ble.Sdk.Models;

namespace Delfin.Ble.Sdk.Events;

/// <summary>
/// Event arguments for connection state change notifications.
/// </summary>
public class ConnectionEventArgs : EventArgs
{
    /// <summary>The affected instrument. Use <see cref="IInstrument.DeviceSerial"/> for the serial.</summary>
    public required IInstrument Instrument { get; init; }

    /// <summary>Current connection state after the transition.</summary>
    public required ConnectionState State { get; init; }
}
