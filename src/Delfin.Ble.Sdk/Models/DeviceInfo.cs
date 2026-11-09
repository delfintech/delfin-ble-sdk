namespace Delfin.Ble.Sdk.Models;

/// <summary>
/// Information about a discovered Delfin BLE device from a scan operation.
/// </summary>
/// <param name="Address">BLE device address.</param>
/// <param name="DeviceSerial">Human-readable device serial (e.g., "VX11234").</param>
/// <param name="SignalStrength">Signal strength in dBm.</param>
public record DeviceInfo(string Address, string DeviceSerial, int SignalStrength);
