namespace Delfin.Ble.Sdk.Events;

/// <summary>
/// Event arguments raised when a scanning error occurs.
/// </summary>
public class ScanErrorEventArgs : EventArgs
{
    /// <summary>Category of the scan error.</summary>
    public required ScanErrorCategory Category { get; init; }

    /// <summary>Human-readable description of the error.</summary>
    public required string Description { get; init; }
}

/// <summary>
/// Categories of errors that can occur during room sensor scanning.
/// </summary>
public enum ScanErrorCategory
{
    /// <summary>The BLE dongle did not respond to a scan command within the expected timeout.</summary>
    DongleUnresponsive,

    /// <summary>The BLE dongle was physically disconnected during scanning.</summary>
    DongleDisconnected
}
