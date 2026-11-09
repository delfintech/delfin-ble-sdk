namespace Delfin.Ble.Sdk.Events;

/// <summary>
/// Event arguments emitted when a device's calibration validity date is reported.
/// </summary>
public class CalibrationDueEventArgs : EventArgs
{
    /// <summary>The instrument. Use <see cref="IInstrument.DeviceSerial"/> for the serial.</summary>
    public required IInstrument Instrument { get; init; }

    /// <summary>Calibration valid until year (e.g., 2025).</summary>
    public required int Year { get; init; }

    /// <summary>Calibration valid until month (1–12).</summary>
    public required int Month { get; init; }
}
