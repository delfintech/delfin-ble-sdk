using Delfin.Ble.Driver;
using Delfin.Ble.Sdk;
using Delfin.Ble.Sdk.Events;

namespace Delfin.Ble.Demo;

class Program
{
    static string T() => DateTime.Now.ToString("HH.mm.ss.fff");

    static async Task Main(string[] args)
    {
        var factory = new DriverFactory();

        // --find-port mode: detect dongle, print port, exit
        if (args.Contains("--find-port"))
        {
            var port = await factory.FindPortAsync();
            if (port != null)
                Console.WriteLine(port);
            else
            {
                Console.Error.WriteLine("No BleuIO dongle found.");
                Environment.ExitCode = 1;
            }
            return;
        }

        Console.WriteLine("Delfin BLE SDK Demo");
        Console.WriteLine("===================");
        Console.WriteLine();

        // Parse command line:
        //   dotnet run -- COM4 VX10001 DCC1423 --sensor C7:96:9A:4A:A5:1F
        //   dotnet run -- /dev/cu.usbmodem4048FDE858F91 VX10001 DCC1423
        //   dotnet run -- DCC1423   (auto-detects port)
        string? portName = null;
        var deviceSerials = new List<string>();
        var sensorMacs = new List<string>();
        bool enableLogging = args.Contains("--log");
        string? logName = null;

        int argStart = 0;
        // Support positional port (first arg) for backward compatibility
        if (args.Length > 0 && IsPortName(args[0]))
        {
            portName = args[0];
            argStart = 1;
        }

        for (int i = argStart; i < args.Length; i++)
        {
            if (args[i].Equals("--port", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                portName = args[++i];
            else if (args[i].Equals("--sensor", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                sensorMacs.Add(args[++i]);
            else if (args[i].Equals("--log", StringComparison.OrdinalIgnoreCase))
            {
                // --log or --log test_1
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--") && !IsPortName(args[i + 1]) && !args[i + 1].Contains(":"))
                    logName = args[++i];
            }
            else if (!args[i].StartsWith("--"))
                deviceSerials.Add(args[i]);
        }

        CsvLogger? logger = enableLogging ? new CsvLogger("./logs", logName) : null;
        if (enableLogging)
            Console.WriteLine($"CSV logging enabled → ./logs/{(logName != null ? logName + "/" : "")}");

        // If no port given, auto-detect once
        if (portName == null)
        {
            Console.Write("Detecting dongle... ");
            portName = await factory.FindPortAsync();
            if (portName == null)
            {
                Console.WriteLine("FAILED");
                Console.WriteLine();
                if (OperatingSystem.IsMacOS())
                {
                    Console.WriteLine("No BleuIO dongle found.");
                    Console.WriteLine("  Check: ls /dev/cu.usbmodem* /dev/cu.usbserial*");
                    Console.WriteLine("  Or specify the port: dotnet run -- /dev/cu.usbmodemXXXXX DCC1423");
                }
                else
                {
                    Console.WriteLine("No BleuIO dongle found. Use --find-port or specify COM port.");
                }
                return;
            }
            Console.WriteLine(portName);
        }

        Console.WriteLine($"Port: {portName}");
        if (deviceSerials.Count > 0) Console.WriteLine($"Devices: {string.Join(", ", deviceSerials)}");
        if (sensorMacs.Count > 0) Console.WriteLine($"Sensors: {string.Join(", ", sensorMacs)}");
        Console.WriteLine();

        // Create client — port is always explicit, no background monitoring needed
        bool rawMode = args.Contains("--raw");

        using var client = factory.CreateClient(new DriverOptions(
            ConfigFilePath: "",
            SerialPortName: portName,
            DeviceSerials: deviceSerials.Count > 0 ? deviceSerials : null,
            SensorMacs: sensorMacs.Count > 0 ? sensorMacs : null,
            RawMode: rawMode
        ));

        if (rawMode) Console.WriteLine("Raw mode: showing all protocol values");
        Console.WriteLine($"Driver version: {client.DriverVersion}");

        // Subscribe to events
        client.ErrorOccurred += (_, e) => Console.WriteLine($"{T()} [ERROR] {e.Category}: {e.Message}");
        client.DongleLost += (_, _) =>
        {
            Console.WriteLine($"{T()} [DONGLE] Lost! Restart the application.");
            Environment.ExitCode = 1;
        };
        client.DongleRecovered += (_, _) => Console.WriteLine($"{T()} [DONGLE] Recovered");

        client.RoomSensorScanner.SensorDataReceived += (_, e) =>
        {
            var r = e.Reading;
            Console.WriteLine($"{T()} [SENSOR] {r.MacAddress}: T={r.Temperature:F1}C, RH={r.Humidity:F1}%{(r.Pressure.HasValue ? $", P={r.Pressure:F0}Pa" : "")}");

            if (logger != null)
            {
                if (r.Temperature.HasValue) logger.LogSensor(r.MacAddress, "temperature", r.Temperature.Value, "C");
                if (r.Humidity.HasValue) logger.LogSensor(r.MacAddress, "humidity", r.Humidity.Value, "%RH");
                if (r.Pressure.HasValue) logger.LogSensor(r.MacAddress, "pressure", r.Pressure.Value, "Pa");
            }
        };

        // Subscribe to instrument events — handles async connections from the continuous scan loop
        var subscribedSerials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        client.InstrumentConnected += (_, e) =>
        {
            if (subscribedSerials.Add(e.Instrument.DeviceSerial))
            {
                SubscribeToInstrument(e.Instrument, logger);
                Console.WriteLine($"{T()} [{e.Instrument.DeviceSerial}] Connected");
            }
            else if (e.IsReconnection)
            {
                Console.WriteLine($"{T()} [{e.Instrument.DeviceSerial}] Reconnected");
            }
        };

        // Initialize — opens port directly, connects devices, starts scanning
        Console.WriteLine("Initializing...");
        try
        {
            await client.InitializeAsync();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Failed: {ex.Message}");
            if (OperatingSystem.IsMacOS())
            {
                Console.WriteLine();
                Console.WriteLine("Troubleshooting:");
                Console.WriteLine("  1. Unplug and re-plug the BleuIO dongle");
                Console.WriteLine("  2. Run: dotnet run -- --diag");
                Console.WriteLine($"  3. Try specifying port directly: dotnet run -- /dev/cu.usbmodemXXXXX {(deviceSerials.Count > 0 ? deviceSerials[0] : "VX10001")}");
            }
            return;
        }

        Console.WriteLine("Ready. Press 'q' to quit.");
        Console.WriteLine();

        // Wait for quit
        while (true)
        {
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
                break;
            await Task.Delay(100);
        }

        await client.DisconnectAllAsync();
        Console.WriteLine("Goodbye!");
    }

    static void SubscribeToInstrument(IInstrument instrument, CsvLogger? logger)
    {
        instrument.MeasurementCompleted += (_, result) =>
        {
            Console.WriteLine();
            Console.WriteLine($"{T()} --- MEASUREMENT {(result.Success ? "COMPLETED" : "CANCELLED")} ---");
            Console.WriteLine($"  Device: {result.DeviceSerial}");

            // Mode is the user-selected mode/probe the device reported. Non-null only for
            // devices with a mode concept (D3N probe, SWL measurement mode), so show it
            // whenever present — no device-type knowledge needed.
            if (!string.IsNullOrEmpty(result.Mode))
            {
                Console.WriteLine($"  Mode: {result.Mode}");
            }

            // SensorId is the interchangeable sensor head id (DMI2 devices), when reported.
            if (!string.IsNullOrEmpty(result.SensorId))
            {
                Console.WriteLine($"  SensorId: {result.SensorId}");
            }

            foreach (var (key, value) in result.Values)
            {
                var unit = result.Units.TryGetValue(key, out var u) ? u : "";
                Console.WriteLine($"    {key} = {value:F2} {unit}");

                logger?.LogMeasurement(result.DeviceSerial, key, value, unit);
            }

            // MeasurementCount is a device-lifetime counter tracked at the instrument level.
            if (instrument.MeasurementCount is int count)
            {
                Console.WriteLine($"  MeasurementCount: {count}");
            }
            Console.WriteLine($"  Session: {result.SessionId:N}");
            Console.WriteLine();
        };

        instrument.SessionStarted += (_, e) =>
            Console.WriteLine($"{T()} [{instrument.DeviceSerial}] Measurement started (session {e.SessionId:N})");
        instrument.SessionCancelled += (_, e) =>
            Console.WriteLine($"{T()} [{instrument.DeviceSerial}] Measurement cancelled (session {e.SessionId:N})");
        instrument.ConnectionLost += (_, _) =>
            Console.WriteLine($"{T()} [{instrument.DeviceSerial}] Connection lost");
        instrument.Reconnected += (_, _) =>
            Console.WriteLine($"{T()} [{instrument.DeviceSerial}] Reconnected");
        instrument.ReconnectionFailed += (_, _) =>
            Console.WriteLine($"{T()} [{instrument.DeviceSerial}] Reconnection failed");
    }

    static bool IsPortName(string arg)
    {
        return arg.StartsWith("/dev/", StringComparison.Ordinal)
            || arg.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
            // Absolute paths to existing files (e.g. socat PTY symlinks like /tmp/delfin_app)
            || (arg.StartsWith('/') && File.Exists(arg));
    }
}
