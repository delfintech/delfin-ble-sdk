namespace Delfin.Ble.Sdk.Models;

/// <summary>
/// Display formatting hint for a measurement value, containing the recommended
/// decimal places and unit string for rendering.
/// </summary>
/// <param name="DecimalPlaces">Recommended number of decimal places for display.</param>
/// <param name="UnitString">Unit string identifier from the device configuration.</param>
public record DisplayHint(int DecimalPlaces, string UnitString);
