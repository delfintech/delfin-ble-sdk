namespace Delfin.Ble.Sdk.Models;

/// <summary>
/// Represents the BLE connection state of an instrument.
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}
