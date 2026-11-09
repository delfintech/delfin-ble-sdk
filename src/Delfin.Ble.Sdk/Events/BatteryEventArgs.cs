using Delfin.Ble.Sdk.Models;

namespace Delfin.Ble.Sdk.Events;

/// <summary>
/// Event arguments for battery status notifications.
/// </summary>
public class BatteryEventArgs : EventArgs
{
    /// <summary>The instrument reporting battery status. Use <see cref="IInstrument.DeviceSerial"/> for the serial.</summary>
    public required IInstrument Instrument { get; init; }

    /// <summary>Classified battery level based on device thresholds.</summary>
    public required BatteryLevel Level { get; init; }

    /// <summary>Raw battery voltage value.</summary>
    public required float Voltage { get; init; }
}
