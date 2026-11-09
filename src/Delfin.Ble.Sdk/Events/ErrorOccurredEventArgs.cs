using Delfin.Ble.Sdk.Models;

namespace Delfin.Ble.Sdk.Events;

/// <summary>
/// Event arguments for runtime errors emitted by the SDK.
/// </summary>
public class ErrorOccurredEventArgs : EventArgs
{
    /// <summary>Category of the error.</summary>
    public required ErrorCategory Category { get; init; }

    /// <summary>Human-readable error message.</summary>
    public required string Message { get; init; }

    /// <summary>Additional context about the error, or null if not available.</summary>
    public string? Context { get; init; }
}
