using Delfin.Ble.Sdk.Models;

namespace Delfin.Ble.Sdk.Events;

/// <summary>
/// Event arguments raised when a previously unseen sensor is discovered during a scanning session.
/// </summary>
public class SensorDiscoveredEventArgs : EventArgs
{
    /// <summary>MAC address of the discovered sensor (format: "AA:BB:CC:DD:EE:FF").</summary>
    public required string MacAddress { get; init; }

    /// <summary>Type of the discovered sensor.</summary>
    public required SensorType SensorType { get; init; }

    /// <summary>Signal strength in dBm at time of discovery, or null if not reported.</summary>
    public int? Rssi { get; init; }
}
