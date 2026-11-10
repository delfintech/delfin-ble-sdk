# Delfin BLE SDK
```

777777777777777777      777777777777777777777  777777                    7777777777777777  777777 7777777777777777777    
77777777777777777777    777777777777777777777  777777                  777777777777777777  777777 777777777777777777777  
7777777777777777777777  777777777777777777777  777777                77777777777777777777  777777 77777777777777777777777
                 777777 7777777                777777                77777                 777777 777777            77777
77777            777777 777777777777777777777  777777                77777777777777777777  777777 777777            77777
77777            777777 777777777777777777777  777777                77777777777777777777  777777 777777            77777
77777            777777 777777777777777777777  777777                77777777777777777777  777777 777777            77777
77777            777777 7777777                777777                77777                 777777 777777            77777
7777777777777777777777   77777777777777777777  77777777777777777777  77777                 777777 777777            77777
77777777777777777777       777777777777777777    777777777777777777  77777                 777777 777777            77777
777777777777777777           7777777777777777       777777777777777  77777                 777777 777777            77777

```


.NET 8 SDK for communicating with Delfin Technologies BLE measurement devices via a BleuIO USB dongle.

## Requirements

- .NET 8.0 SDK or later (.NET 9, .NET 10, etc. are fully supported)
- Windows 10/11 or macOS
- BleuIO USB BLE dongle
- Delfin measurement device (VX10001, DCC1423, SWL, MSC, etc.) or use the device simulator (requires com0com on Windows or socat on macOS/Linux, see chapter Device Simulator)

> **Platform notes:**
> - WinForms demos are Windows-only
> - Console demo and Avalonia demo work on both Windows and macOS
> - Device simulator works on Windows (com0com) and macOS/Linux (socat)

## Quick Start

```bash
# Build
dotnet build Delfin.Ble.sln

# Find the BleuIO dongle COM port
dotnet run --project src/Delfin.Ble.Demo -- --find-port
# Output: COM4

# Run with devices and room sensor
dotnet run --project src/Delfin.Ble.Demo -c Release -- --port COM4 VX10001 DCC1423 --sensor C7:96:9A:4A:A5:1F
```

> **Note:** The `--` before `--port` is required by `dotnet run` to separate its own flags from the app's arguments. If running the compiled exe directly, omit it:
> ```
> Delfin.Ble.Demo.exe --port COM4 VX10001 DCC1423 --sensor C7:96:9A:4A:A5:1F
> ```

### Command Line Arguments

```
Delfin.Ble.Demo.exe [DEVICE_SERIAL...] [--port COMx] [--sensor MAC...] [--log [name]]

  --port COMx        COM port (preferred)
  COMx               COM port as first positional arg (also supported)
  DEVICE_SERIAL      Device serial numbers to auto-connect
  --sensor MAC       Room sensor MAC to monitor (Delfin sensor or RuuviTag).
                     Repeatable — pass once per sensor. Omit to monitor ALL sensors.
  --find-port        Detect dongle port, print it, exit
  --log [subfolder]  Enable CSV logging to ./logs/[subfolder] folder
```

Room sensor examples:

```
# Monitor ALL room sensors (no --sensor given)
Delfin.Ble.Demo.exe --port COM4 --log room

# Monitor only specific sensors (repeat --sensor per MAC)
Delfin.Ble.Demo.exe --port COM4 --sensor C7:96:9A:4A:A5:1F --sensor C3:50:68:AE:F9:A4 --log room
```

### CSV Logging

When `--log` is specified, all measurement values and sensor readings are saved to CSV files:

```
./logs/
  VX10001/
    tewl.csv
    skin_temperature.csv
    humidity.csv
    battery_voltage.csv
  DCC1423/
    erythema.csv
    melanin.csv
    ita_angle.csv
  C7_96_9A_4A_A5_1F/
    temperature.csv
    humidity.csv
    pressure.csv
```

Each CSV contains: `timestamp,value,unit`

## Integration Example

```csharp
using Delfin.Ble.Driver;
using Delfin.Ble.Sdk;
using Delfin.Ble.Sdk.Events;

var factory = new DriverFactory();

// Detect dongle port (run once, save the result)
var port = await factory.FindPortAsync();

// Create client
using var client = factory.CreateClient(new DriverOptions(
    ConfigFilePath: "",
    SerialPortName: port,
    DeviceSerials: new[] { "VX10001", "DCC1423" },
    SensorMacs: new[] { "C7:96:9A:4A:A5:1F" }
));

// Subscribe to instrument connections
client.InstrumentConnected += (_, e) =>
{
    Console.WriteLine($"[{e.Instrument.DeviceSerial}] Connected");

    e.Instrument.MeasurementCompleted += (_, result) =>
    {
        Console.WriteLine($"Measurement from {result.DeviceSerial}:");
        foreach (var (key, value) in result.Values)
        {
            var unit = result.Units.TryGetValue(key, out var u) ? u : "";
            Console.WriteLine($"  {key} = {value:F2} {unit}");
        }
    };
};

// Subscribe to room sensor data
client.RoomSensorScanner.SensorDataReceived += (_, e) =>
{
    var r = e.Reading;
    Console.WriteLine($"Sensor {r.MacAddress}: T={r.Temperature:F1}C, RH={r.Humidity:F1}%");
};

// Initialize — connects to devices automatically
await client.InitializeAsync();

// Measurements flow via events. Keep the app alive.
Console.ReadKey();

await client.DisconnectAllAsync();
```

## Integration Example (VB.NET)

```vb
Imports Delfin.Ble.Driver
Imports Delfin.Ble.Sdk
Imports Delfin.Ble.Sdk.Events

Module Program
    Sub Main()
        MainAsync().GetAwaiter().GetResult()
    End Sub

    Async Function MainAsync() As Task
        Dim factory As New DriverFactory()

        ' Detect dongle port
        Dim port = Await factory.FindPortAsync()

        ' Create client
        Dim options As New DriverOptions(
            ConfigFilePath:="",
            SerialPortName:=port,
            DeviceSerials:={"VX10001", "DCC1423"},
            SensorMacs:={"C7:96:9A:4A:A5:1F"})

        Using client = factory.CreateClient(options)

            ' Subscribe to instrument connections
            AddHandler client.InstrumentConnected, Sub(sender, e)
                Console.WriteLine($"[{e.Instrument.DeviceSerial}] Connected")

                AddHandler e.Instrument.MeasurementCompleted, Sub(s, result)
                    Console.WriteLine($"Measurement from {result.DeviceSerial}:")
                    For Each kvp In result.Values
                        Dim unit = ""
                        result.Units.TryGetValue(kvp.Key, unit)
                        Console.WriteLine($"  {kvp.Key} = {kvp.Value:F2} {unit}")
                    Next
                End Sub
            End Sub

            ' Subscribe to room sensor data
            AddHandler client.RoomSensorScanner.SensorDataReceived, Sub(sender, e)
                Dim r = e.Reading
                Console.WriteLine($"Sensor {r.MacAddress}: T={r.Temperature:F1}C, RH={r.Humidity:F1}%")
            End Sub

            ' Initialize — connects to devices automatically
            Await client.InitializeAsync()

            Console.WriteLine("Ready. Press any key to quit.")
            Console.ReadKey()

            Await client.DisconnectAllAsync()
        End Using
    End Function
End Module
```

## Architecture

```
Delfin.Ble.Sdk       Source code — interfaces, events, models
Delfin.Ble.Driver    Compiled DLL — BLE transport and protocol logic
Delfin.Ble.Demo      Source code — reference implementation
```

The SDK defines the public API. The Driver implements it. Your application references both.

## Key Interfaces

### IDeviceClient

Main entry point. Manages dongle lifecycle and device connections.

| Member | Description |
|--------|-------------|
| `InitializeAsync()` | Opens dongle, connects devices, starts scanning |
| `Instruments` | Dictionary of connected instruments by serial |
| `RoomSensorScanner` | Access to BLE room sensor scanning |
| `InstrumentConnected` | Event fired when a device connects |
| `DongleLost` | Event fired if USB dongle is unplugged |
| `DisconnectAllAsync()` | Clean shutdown |

### IInstrument

Represents a connected measurement device.

| Member | Description |
|--------|-------------|
| `DeviceSerial` | Device serial number (e.g. "VX10001") |
| `State` | Connection state |
| `MeasurementCompleted` | Event with measurement results |
| `SessionStarted` | Event when measurement begins |
| `ConnectionLost` | Event when device disconnects |
| `Reconnected` | Event when device reconnects |

### MeasurementResult

Returned via `MeasurementCompleted` event.

| Property | Description |
|----------|-------------|
| `DeviceSerial` | Which device produced this result |
| `Success` | Whether measurement completed normally |
| `Values` | Dictionary of measured values (key → double) |
| `Units` | Dictionary of units (key → string) |
| `SensorId` | Interchangeable sensor head id, hex (DMI2 devices, if available) |
| `Mode` | Active mode/probe (e.g. "sda", "nai" for SWL; "L50"/"XS5" probe for D3N) |

### Instrument-level properties

Some values are device state, not per-measurement, and are read from `IInstrument` rather than a `MeasurementResult`:

| Property | Description |
|----------|-------------|
| `Mode` | Active mode/probe the device reports |
| `SensorId` | Interchangeable sensor head id (DMI2 devices) |
| `MeasurementCount` | Device-lifetime measurement counter (tracked at instrument level — the count message can arrive before or after a measurement, so it is not part of a single result) |
| `BatteryVoltage` / `BatteryStatus` | Latest battery reading |

## Measurement Values by Device

### VapoMeter (VX1)
| Key | Unit | Description |
|-----|------|-------------|
| tewl | g/m²/h | Trans-epidermal water loss |
| skin_temperature | °C | Skin surface temperature |
| battery_voltage | V | Battery voltage |

### VapoMeter (SWL)
| Key | Unit | Description |
|-----|------|-------------|
| tewl | g/m²/h | Trans-epidermal water loss |
| battery_voltage | V | Battery voltage |


### SkinColorCatch (DCC)
| Key | Unit | Description |
|-----|------|-------------|
| erythema | AU | Erythema index |
| melanin | AU | Melanin index |
| ita_angle | ° | ITA angle |
| lab_l, lab_a, lab_b | - | CIE L*a*b* color space |
| chroma | - | Chroma |
| hue_angle | ° | Hue angle |
| color_r, color_g, color_b | - | RGB color values |
| tristimulus_r, tristimulus_g, tristimulus_b | - | Tristimulus RGB |
| tristimulus_x, tristimulus_y, tristimulus_z | - | Tristimulus XYZ |
| battery_voltage | V | Battery voltage |



### MoistureMeterSC (MSC)
| Key | Unit | Description |
|-----|------|-------------|
| sc_hydration | AU | Stratum corneum hydration |
| force | g | Contact force |
| battery_voltage | V | Battery voltage |

### MoistureMeterEpiD (MDE)
| Key | Unit | Description |
|-----|------|-------------|
| permittivity | - | Skin permittivity (TDC) |
| pwc | % | Percentage Water Content (calculated) |
| force | g | Contact force |
| battery_voltage | V | Battery voltage |

### MoistureMeterD Compact (MDC)
| Key | Unit | Description |
|-----|------|-------------|
| permittivity | - | Skin permittivity (TDC) |
| pwc | % | Percentage Water Content (calculated) |
| force | g | Contact force |
| battery_voltage | V | Battery voltage |

### LymphScanner (MDL)
| Key | Unit | Description |
|-----|------|-------------|
| permittivity | - | Skin permittivity (TDC) |
| pwc | % | Percentage Water Content (calculated) |
| force | g | Contact force |
| battery_voltage | V | Battery voltage |

* Only the LymphScanner "spot" mode sends out data

### MoistureMeterD (D3N)
| Key | Unit | Description |
|-----|------|-------------|
| permittivity | - | Skin permittivity (TDC) |
| pwc | % | Percentage Water Content (calculated) |
| battery_voltage | V | Battery voltage |

### ElastiMeter (ELM)
| Key | Unit | Description |
|-----|------|-------------|
| skin_elasticity | N/m | Skin elasticity |
| battery_voltage | V | Battery voltage |

### SkinFibroMeter (SFM)
| Key | Unit | Description |
|-----|------|-------------|
| indentation_force | N | Indentation force |
| battery_voltage | V | Battery voltage |

### SkinGlossMeter (SGM)
| Key | Unit | Description |
|-----|------|-------------|
| gloss | SGU | Skin gloss value |
| gloss_max | AU | Gloss maximum |
| battery_voltage | V | Battery voltage |

### SebumScale (SSC)
| Key | Unit | Description |
|-----|------|-------------|
| sebum_mass | µg/cm² | Sebum mass |
| battery_voltage | V | Battery voltage |


## Room Sensor

The SDK continuously monitors room sensor data (Delfin sensors and RuuviTags) alongside any connected measurement devices — scanning runs in parallel with GATT connections:

| Property | Unit | Description |
|----------|------|-------------|
| Temperature | °C | Room temperature |
| Humidity | % | Room relative humidity |
| Pressure | Pa | Atmospheric pressure |

Specify `--sensor MAC` (repeatable) to monitor only those sensors. If no `--sensor` is given, all detected room sensors are monitored. Readings are delivered at most once per sensor every 10 seconds.

## Supported Devices

Delfin devices with BLE (Bluetooth LE) communication.
VapoMeter VX, 
VapoMeter SWL5, 
SkinColorCatch,
MoistureMeterSC,
MoistureMeterEpiD,
MoistureMeterD (XS5,S15,M25 & L50 probes)
MoistureMeterD Compact
ElastiMeter,
SkinFibroMeter,
SkinGlossMeter,
SebumScale

## WinForms Demo Applications

Included are complete WinForms demo apps in both C# and VB.NET:

```bash
# C# version (Windows only)
dotnet run --project src/Delfin.Ble.WinFormsDemo

# VB.NET version (Windows only)
dotnet run --project src/Delfin.Ble.WinFormsDemo.VB
```

Both demos provide:
- COM port and device serial configuration fields
- Connect / Disconnect buttons
- Green/red status indicators per device
- Live measurement grid (parameter, value, unit)
- Room sensor readings with MAC address
- Event log with timestamps

The WinForms demos show how to safely marshal BLE events to the UI thread using `Invoke()` and how to properly release the COM port on application exit.

## Avalonia Demo (Cross-Platform)

A cross-platform GUI demo using Avalonia UI — runs on Windows and macOS:

```bash
dotnet run --project src/Delfin.Ble.AvaloniaDemo
```

Same features as the WinForms demos but works on all platforms. Uses the Fluent theme for a modern look.

## Demo Projects Overview

| Project | Language | Platform | UI |
|---------|----------|----------|-----|
| Delfin.Ble.Demo | C# | Windows/macOS | Console |
| Delfin.Ble.WinFormsDemo | C# | Windows | WinForms |
| Delfin.Ble.WinFormsDemo.VB | VB.NET | Windows | WinForms |
| Delfin.Ble.AvaloniaDemo | C# | Windows/macOS | Avalonia |

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "No BleuIO dongle found" | Check USB connection, run `--find-port` |
| Device not connecting | Ensure device is powered on and in range |
| Dongle lost during operation | Replug dongle and restart application |
| Slow connection | Some devices advertise infrequently — wait up to 10s |

## Support

For technical support, contact Delfin Technologies:

- Email: info@delfintech.com
- Web: https://www.delfintech.com

## License

This kit contains components under different licenses. See `LICENSE.txt` for the overview.

| Component | License |
|-----------|---------|
| `lib/Delfin.Ble.Driver.dll`, simulator tool in `tools/` | Delfin End User License Agreement — `EULA.txt` |
| SDK interfaces (`src/Delfin.Ble.Sdk`, `lib/Delfin.Ble.Sdk.dll`) and all demo source | MIT — `LICENSE-MIT.txt` |
| Third-party components (System.IO.Ports, System.Management, ...) | Their own licenses — `THIRD-PARTY-NOTICES.txt` |

You may embed the Driver in your own applications and distribute it, unmodified, as part of them (see the EULA for the exact terms). By installing or using the Driver you accept the EULA.

## Device Simulator

For development without hardware, a simulator is included. It acts as a fake BleuIO dongle on one side of a virtual serial port pair. You point your app at the other side.

### Windows Setup (com0com)

1. **Install com0com** (virtual serial port driver):
   - Download signed version from [com0com.com](https://com0com.com/) or [SourceForge](https://sourceforge.net/projects/com0com/)
   - Run installer, create a port pair (e.g. COM20↔COM21)
   - Verify ports exist: `[System.IO.Ports.SerialPort]::GetPortNames()`

2. **Run the simulator** on one side of the pair:
   ```bash
   tools\Delfin.Ble.SimulatorDemo.exe COM20 VX10001 DCC1423
   ```

3. **Run your app** pointing at the other side:
   ```bash
   dotnet run --project src/Delfin.Ble.Demo -c Release -- COM21 VX10001 DCC1423
   ```

### macOS / Linux Setup (socat)

`socat` is the cross-platform equivalent of com0com. It creates a pair of linked pseudo-terminals (PTYs).

1. **Install socat.** On macOS:
   ```bash
   brew install socat
   ```
   On Linux (Debian/Ubuntu):
   ```bash
   sudo apt install socat
   ```

2. **Create the virtual port pair** using the helper script (leave it running). Unzipping may drop the executable bit, so set it once:
   ```bash
   chmod +x tools/socat-loopback.sh
   ```
   ```bash
   ./tools/socat-loopback.sh
   ```
   This creates two stable paths: `/tmp/delfin_sim` (simulator side) and `/tmp/delfin_app` (app side).

3. **Run the simulator and your app** in two more terminals:
   ```bash
   dotnet tools/Delfin.Ble.SimulatorDemo.dll /tmp/delfin_sim VX10001 DCC1423
   dotnet run --project src/Delfin.Ble.Demo -- /tmp/delfin_app VX10001 DCC1423
   ```
   `tools/Delfin.Ble.SimulatorDemo.exe` is the Windows launcher only. On macOS/Linux always start the simulator with `dotnet` and the `.dll`, as shown.

The Avalonia GUI demo also works with the simulator — enter `/tmp/delfin_app` in the Port field.

> **Note:** On macOS/Linux, `System.IO.Ports.SerialPort` cannot open PTY devices. The driver
> automatically detects non-`/dev/cu.*` paths and opens them as raw file streams instead.
> Real BleuIO hardware still uses the `/dev/cu.usbmodem*` path as normal.

### Simulator Commands

While the simulator is running, type commands to control it:

| Command | Description |
|---------|-------------|
| `m VX10001` | Trigger a measurement session on VX10001 |
| `m DCC1423` | Trigger a measurement session on DCC1423 |
| `d VX10001` | Simulate device disconnect |
| `q` | Quit simulator |

### Use Cases

- **App development** — build and test your integration without physical devices
- **IQ/OQ validation** — trigger known measurement values, verify output matches expected
- **CI/CD** — automated testing in build pipelines with no hardware dependencies
- **Demo/training** — show SDK functionality without device setup

### Supported Simulated Devices

VX1, SWL, DCC, SPT, MSC, SGM, D3N, MDE, MDC, MDL, ELM, SFM, SSC

Pass any serial (e.g. `VX10001`, `DCC1423`, `SWL5001`) and the simulator auto-configures the correct measurement protocol.
