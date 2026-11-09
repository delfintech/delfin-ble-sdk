using System.Globalization;

namespace Delfin.Ble.Demo;

/// <summary>
/// Logs measurement values and sensor readings to CSV files.
/// Structure: ./logs/{device_serial_or_mac}/{parameter}.csv
/// Each CSV has header: timestamp,value,unit
/// </summary>
internal class CsvLogger
{
    private readonly string _baseDir;
    private readonly object _lock = new();

    public CsvLogger(string baseDir = "./logs", string? subFolder = null)
    {
        _baseDir = subFolder != null ? Path.Combine(baseDir, SanitizePath(subFolder)) : baseDir;
    }

    /// <summary>
    /// Logs a single measurement value to CSV.
    /// File: ./logs/{deviceSerial}/{parameter}.csv
    /// </summary>
    public void LogMeasurement(string deviceSerial, string parameter, float value, string unit)
    {
        var dir = Path.Combine(_baseDir, SanitizePath(deviceSerial));
        var file = Path.Combine(dir, $"{SanitizePath(parameter)}.csv");
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var line = $"{timestamp},{value.ToString("G", CultureInfo.InvariantCulture)},{unit}";

        WriteLineToFile(file, dir, "timestamp,value,unit", line);
    }

    /// <summary>
    /// Logs a sensor reading to CSV.
    /// File: ./logs/{mac}/{parameter}.csv
    /// </summary>
    public void LogSensor(string macAddress, string parameter, double value, string unit)
    {
        var dir = Path.Combine(_baseDir, SanitizePath(macAddress));
        var file = Path.Combine(dir, $"{SanitizePath(parameter)}.csv");
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var line = $"{timestamp},{value.ToString("G", CultureInfo.InvariantCulture)},{unit}";

        WriteLineToFile(file, dir, "timestamp,value,unit", line);
    }

    private void WriteLineToFile(string file, string dir, string header, string line)
    {
        lock (_lock)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            bool writeHeader = !File.Exists(file);
            using var writer = new StreamWriter(file, append: true);
            if (writeHeader)
                writer.WriteLine(header);
            writer.WriteLine(line);
        }
    }

    private static string SanitizePath(string name)
    {
        // Replace characters that are invalid in file/folder names
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
