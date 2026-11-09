using Delfin.Ble.Sdk.Models;

namespace Delfin.Ble.Sdk.Events;

/// <summary>
/// Event arguments raised when a room sensor reading passes filtering and rate limiting.
/// </summary>
public class RoomSensorReadingEventArgs : EventArgs
{
    /// <summary>The unified sensor reading that passed all filters.</summary>
    public required RoomSensorReading Reading { get; init; }
}
