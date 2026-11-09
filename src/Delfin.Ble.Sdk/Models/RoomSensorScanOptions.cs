using System.Text.RegularExpressions;

namespace Delfin.Ble.Sdk.Models;

/// <summary>
/// Configuration options for room sensor scanning.
/// </summary>
public partial record RoomSensorScanOptions
{
    private static readonly TimeSpan MinInterval = TimeSpan.Zero;
    private static readonly TimeSpan MaxInterval = TimeSpan.FromSeconds(300);
    private const int MaxMacFilterEntries = 100;

    // MAC address pattern: 6 hex octets separated by colons or hyphens
    private static readonly Regex MacPattern = MacAddressRegex();

    private readonly TimeSpan _rateLimitInterval = TimeSpan.FromSeconds(10);
    private readonly IReadOnlyList<string>? _macFilter;

    /// <summary>
    /// Minimum interval between forwarding readings from the same sensor.
    /// Prevents RuuviTag flood (1/sec). Default: 10 seconds.
    /// Set to TimeSpan.Zero to disable rate limiting.
    /// Valid range: 0–300 seconds.
    /// </summary>
    public TimeSpan RateLimitInterval
    {
        get => _rateLimitInterval;
        init
        {
            if (value < MinInterval || value > MaxInterval)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(RateLimitInterval),
                    value,
                    $"RateLimitInterval must be between {MinInterval.TotalSeconds} and {MaxInterval.TotalSeconds} seconds.");
            }

            _rateLimitInterval = value;
        }
    }

    /// <summary>
    /// Initial MAC/ID filter. Only sensors with these addresses will be reported.
    /// Empty or null means accept all sensors.
    /// MAC format: "AA:BB:CC:DD:EE:FF" (case-insensitive, colon or hyphen separators).
    /// Maximum 100 entries.
    /// </summary>
    public IReadOnlyList<string>? MacFilter
    {
        get => _macFilter;
        init
        {
            if (value is not null)
            {
                if (value.Count > MaxMacFilterEntries)
                {
                    throw new ArgumentException(
                        $"MacFilter cannot contain more than {MaxMacFilterEntries} entries.",
                        nameof(MacFilter));
                }

                for (int i = 0; i < value.Count; i++)
                {
                    if (!IsValidMac(value[i]))
                    {
                        throw new ArgumentException(
                            $"MacFilter entry at index {i} is not a valid MAC address: '{value[i]}'.",
                            nameof(MacFilter));
                    }
                }
            }

            _macFilter = value;
        }
    }

    /// <summary>
    /// Validates whether a string is a valid MAC address (XX:XX:XX:XX:XX:XX or XX-XX-XX-XX-XX-XX).
    /// </summary>
    public static bool IsValidMac(string? mac)
    {
        if (string.IsNullOrWhiteSpace(mac))
            return false;

        return MacPattern.IsMatch(mac);
    }

    [GeneratedRegex(@"^[0-9A-Fa-f]{2}([:\-])[0-9A-Fa-f]{2}\1[0-9A-Fa-f]{2}\1[0-9A-Fa-f]{2}\1[0-9A-Fa-f]{2}\1[0-9A-Fa-f]{2}$")]
    private static partial Regex MacAddressRegex();
}
