using System.Text.RegularExpressions;

namespace Delfin.Ble.Sdk.Models;

/// <summary>
/// Unified environmental sensor reading from any supported sensor type.
/// </summary>
public record RoomSensorReading
{
    private static readonly Regex MacRegex = new(
        @"^[0-9A-Fa-f]{2}:[0-9A-Fa-f]{2}:[0-9A-Fa-f]{2}:[0-9A-Fa-f]{2}:[0-9A-Fa-f]{2}:[0-9A-Fa-f]{2}$",
        RegexOptions.Compiled);

    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(30);
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromSeconds(60);

    private const double MinTemperature = -40.0;
    private const double MaxTemperature = 85.0;
    private const double MinHumidity = 0.0;
    private const double MaxHumidity = 100.0;
    private const double MinPressure = 50000.0;
    private const double MaxPressure = 115535.0;

    /// <summary>MAC address of the sensor (format: "AA:BB:CC:DD:EE:FF").</summary>
    public required string MacAddress { get; init; }

    /// <summary>Source sensor type (Delfin proprietary or RuuviTag).</summary>
    public required SensorType SensorType { get; init; }

    /// <summary>Temperature in degrees Celsius. Null if not available.</summary>
    public double? Temperature { get; init; }

    /// <summary>Relative humidity in %RH. Null if not available.</summary>
    public double? Humidity { get; init; }

    /// <summary>Atmospheric pressure in Pa. Null if not available (Delfin sensors don't report this).</summary>
    public double? Pressure { get; init; }

    /// <summary>
    /// Battery level as percentage (0-100) for Delfin sensors,
    /// or voltage in mV for RuuviTags. Null if not available.
    /// </summary>
    public double? BatteryLevel { get; init; }

    /// <summary>Whether BatteryLevel is a percentage (true) or voltage in mV (false).</summary>
    public bool BatteryIsPercentage { get; init; }

    /// <summary>Signal strength in dBm. Null if not reported.</summary>
    public int? Rssi { get; init; }

    /// <summary>When this reading was captured (UTC).</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Optional friendly name or serial for the sensor.
    /// For Delfin sensors this is the serial number; for RuuviTags it may be empty.
    /// </summary>
    public string? SensorName { get; init; }

    /// <summary>
    /// Validates the reading and returns a result indicating success or failure with details.
    /// </summary>
    public ValidationResult Validate() => Validate(DateTimeOffset.UtcNow);

    /// <summary>
    /// Validates the reading against the specified current time.
    /// Useful for deterministic testing or when the system clock differs from the reading source.
    /// </summary>
    public ValidationResult Validate(DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(MacAddress) || !MacRegex.IsMatch(MacAddress))
            return ValidationResult.Failed(nameof(MacAddress), "MacAddress must be in the format XX:XX:XX:XX:XX:XX with hexadecimal characters.");

        if (!Enum.IsDefined(typeof(SensorType), SensorType))
            return ValidationResult.Failed(nameof(SensorType), "SensorType must be Delfin or RuuviTag.");

        if (Temperature is not null && (Temperature < MinTemperature || Temperature > MaxTemperature))
            return ValidationResult.Failed(nameof(Temperature), $"Temperature must be between {MinTemperature} and {MaxTemperature} °C.");

        if (Humidity is not null && (Humidity < MinHumidity || Humidity > MaxHumidity))
            return ValidationResult.Failed(nameof(Humidity), $"Humidity must be between {MinHumidity} and {MaxHumidity} %RH.");

        if (Pressure is not null && (Pressure < MinPressure || Pressure > MaxPressure))
            return ValidationResult.Failed(nameof(Pressure), $"Pressure must be between {MinPressure} and {MaxPressure} Pa.");

        var age = now - Timestamp;
        if (age > MaxAge)
            return ValidationResult.Failed(nameof(Timestamp), "Timestamp must not be older than 30 days.");

        if (Timestamp > now + FutureTolerance)
            return ValidationResult.Failed(nameof(Timestamp), "Timestamp must not be in the future beyond 60 seconds tolerance.");

        if (Temperature is null && Humidity is null && Pressure is null)
            return ValidationResult.Failed("SensorValue", "At least one sensor value (Temperature, Humidity, or Pressure) must be present.");

        return ValidationResult.Success;
    }
}

/// <summary>
/// Result of a RoomSensorReading validation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>A successful validation result.</summary>
    public static readonly ValidationResult Success = new(true, null, null);

    /// <summary>Whether validation passed.</summary>
    public bool IsValid { get; }

    /// <summary>The name of the field that failed validation, or null if valid.</summary>
    public string? FailedField { get; }

    /// <summary>A human-readable error message, or null if valid.</summary>
    public string? ErrorMessage { get; }

    private ValidationResult(bool isValid, string? failedField, string? errorMessage)
    {
        IsValid = isValid;
        FailedField = failedField;
        ErrorMessage = errorMessage;
    }

    /// <summary>Creates a failed validation result.</summary>
    public static ValidationResult Failed(string failedField, string errorMessage)
        => new(false, failedField, errorMessage);
}
