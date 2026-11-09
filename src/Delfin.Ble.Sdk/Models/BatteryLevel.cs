namespace Delfin.Ble.Sdk.Models;

/// <summary>
/// Battery charge level classification based on device-configured voltage thresholds.
/// </summary>
public enum BatteryLevel
{
    Full,
    Adequate,
    Low,
    Empty
}
