namespace Delfin.Ble.Sdk.Models;

/// <summary>
/// Complete measurement result delivered when a session finishes.
/// Values are stored as a flexible dictionary keyed by human-readable names
/// (e.g., "tewl", "skin_temperature", "battery_voltage").
/// </summary>
public class MeasurementResult
{
    /// <summary>Device serial that produced this measurement.</summary>
    public required string DeviceSerial { get; init; }

    /// <summary>Unique session identifier.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>When the measurement session started.</summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>When the measurement session completed.</summary>
    public required DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// The active mode/probe the user selected on the device for this session, or null if
    /// the device has no mode concept. On MoistureMeterD (D3N) this is the probe
    /// (XS5/S15/M25/L50); on VapoMeter SWL it is the measurement mode (nai/sda/eye).
    /// Non-null means it is meaningful — consumers can display it directly with no
    /// device-type knowledge.
    /// </summary>
    public string? Mode { get; init; }

    /// <summary>Interchangeable sensor identifier (hex) from DMI2 Data message 0x01, or null if not received.</summary>
    public string? SensorId { get; init; }

    /// <summary>Calibration validity as YYMM string (e.g., "2703" = March 2027).</summary>
    public string? CalibrationValid { get; init; }

    /// <summary>
    /// All numeric measurement values keyed by readable name.
    /// Keys are config-driven (e.g., "tewl", "skin_temperature", "humidity").
    /// New message types appear automatically without code changes.
    /// </summary>
    public required IReadOnlyDictionary<string, float> Values { get; init; }

    /// <summary>
    /// Unit strings for each value key (e.g., "tewl" → "g/m²/h", "skin_temperature" → "°C").
    /// </summary>
    public required IReadOnlyDictionary<string, string> Units { get; init; }

    /// <summary>
    /// Whether the session completed normally (true) or was cancelled/failed (false).
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>Reason for cancellation or failure, if applicable.</summary>
    public string? CancelReason { get; init; }
}
