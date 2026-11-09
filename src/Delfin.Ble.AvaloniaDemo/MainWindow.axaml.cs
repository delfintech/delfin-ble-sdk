using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Delfin.Ble.Driver;
using Delfin.Ble.Sdk;
using Delfin.Ble.Sdk.Events;
using Delfin.Ble.Sdk.Models;

namespace Delfin.Ble.AvaloniaDemo;

public partial class MainWindow : Window
{
    private readonly DriverFactory _factory = new();
    private IDeviceClient? _client;
    private CancellationTokenSource? _cts;
    private readonly Dictionary<string, TextBlock> _deviceLabels = new();
    private readonly Dictionary<string, IInstrument> _instruments = new();
    private readonly ObservableCollection<MeasurementRow> _measurements = new();
    private readonly ObservableCollection<string> _logEntries = new();

    // UI controls (found by name from XAML)
    private TextBox _txtPort = null!;
    private TextBox _txtDevices = null!;
    private TextBox _txtSensors = null!;
    private Button _btnConnect = null!;
    private Button _btnDisconnect = null!;
    private TextBlock _lblStatus = null!;
    private WrapPanel _pnlDevices = null!;
    private DataGrid _gridMeasurements = null!;
    private ListBox _lstLog = null!;

    public MainWindow()
    {
        InitializeComponent();

        // Resolve named controls
        _txtPort = this.FindControl<TextBox>("TxtPort")!;
        _txtDevices = this.FindControl<TextBox>("TxtDevices")!;
        _txtSensors = this.FindControl<TextBox>("TxtSensors")!;
        _btnConnect = this.FindControl<Button>("BtnConnect")!;
        _btnDisconnect = this.FindControl<Button>("BtnDisconnect")!;
        _lblStatus = this.FindControl<TextBlock>("LblStatus")!;
        _pnlDevices = this.FindControl<WrapPanel>("PnlDevices")!;
        _gridMeasurements = this.FindControl<DataGrid>("GridMeasurements")!;
        _lstLog = this.FindControl<ListBox>("LstLog")!;

        // Bind data
        _gridMeasurements.ItemsSource = _measurements;
        _lstLog.ItemsSource = _logEntries;

        // Wire events
        _btnConnect.Click += BtnConnect_Click;
        _btnDisconnect.Click += BtnDisconnect_Click;
    }

    private async void BtnConnect_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _btnConnect.IsEnabled = false;
        _lblStatus.Text = "Connecting...";
        _lblStatus.Foreground = Brushes.Orange;

        try
        {
            var port = _txtPort.Text?.Trim();
            var devices = (_txtDevices.Text ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            var sensors = (_txtSensors.Text ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            // Auto-detect port if empty
            if (string.IsNullOrEmpty(port))
            {
                Log("Detecting dongle...");
                port = await _factory.FindPortAsync();
                if (port == null)
                {
                    Log("ERROR: No BleuIO dongle found!");
                    _lblStatus.Text = "No dongle found";
                    _lblStatus.Foreground = Brushes.Red;
                    _btnConnect.IsEnabled = true;
                    return;
                }
                _txtPort.Text = port;
                Log($"Found dongle on {port}");
            }

            _client = _factory.CreateClient(new DriverOptions(
                ConfigFilePath: "",
                SerialPortName: port,
                DeviceSerials: devices.Count > 0 ? devices : null,
                SensorMacs: sensors.Count > 0 ? sensors : null
            ));

            // Wire SDK events
            _client.InstrumentConnected += Client_InstrumentConnected;
            _client.DongleLost += (_, _) => RunOnUI(() =>
            {
                Log("DONGLE LOST! Replug and restart.");
                _lblStatus.Text = "Dongle lost!";
                _lblStatus.Foreground = Brushes.Red;
            });
            _client.RoomSensorScanner.SensorDataReceived += Client_SensorDataReceived;

            _cts = new CancellationTokenSource();
            await _client.InitializeAsync(_cts.Token);

            _lblStatus.Text = $"Connected ({port})";
            _lblStatus.Foreground = Brushes.Green;
            _btnDisconnect.IsEnabled = true;

            if (devices.Count > 0)
                Log($"Initialized on {port}. Scanning for: {string.Join(", ", devices)}");
            else
                Log($"Initialized on {port}. No devices specified — sensor scanning only. Type serials in Devices field to connect instruments.");
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            _lblStatus.Text = "Failed";
            _lblStatus.Foreground = Brushes.Red;
            _btnConnect.IsEnabled = true;
        }
    }

    private async void BtnDisconnect_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_client != null)
        {
            _cts?.Cancel();
            try { await _client.DisconnectAllAsync(); } catch { }
            _client.Dispose();
            _client = null;
            _cts?.Dispose();
            _cts = null;
        }

        _lblStatus.Text = "Disconnected";
        _lblStatus.Foreground = Brushes.Gray;
        _btnConnect.IsEnabled = true;
        _btnDisconnect.IsEnabled = false;
        _pnlDevices.Children.Clear();
        _deviceLabels.Clear();
        Log("Disconnected.");
    }

    private void Client_InstrumentConnected(object? sender, InstrumentConnectedEventArgs e)
    {
        RunOnUI(() =>
        {
            var serial = e.Instrument.DeviceSerial;
            var instrument = e.Instrument;
            _instruments[serial] = instrument;

            if (!_deviceLabels.ContainsKey(serial))
            {
                var lbl = new TextBlock
                {
                    Text = $"● {serial}",
                    Foreground = Brushes.Green,
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 15, 0),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                _deviceLabels[serial] = lbl;
                _pnlDevices.Children.Add(lbl);

                // Subscribe to measurement events (once per device)
                instrument.MeasurementCompleted += (_, result) => RunOnUI(() => { OnMeasurement(result); UpdateDeviceLabel(serial); });
                instrument.SessionStarted += (_, ev) => RunOnUI(() =>
                    Log($"[{serial}] Measurement started"));
                instrument.BatteryLow += (_, _) => RunOnUI(() => UpdateDeviceLabel(serial));
                instrument.BatteryEmpty += (_, _) => RunOnUI(() => UpdateDeviceLabel(serial));
                instrument.ConnectionLost += (_, _) => RunOnUI(() =>
                {
                    if (_deviceLabels.TryGetValue(serial, out var l)) l.Foreground = Brushes.Red;
                    Log($"[{serial}] Connection lost");
                });
                instrument.Reconnected += (_, _) => RunOnUI(() =>
                {
                    if (_deviceLabels.TryGetValue(serial, out var l)) l.Foreground = Brushes.Green;
                    Log($"[{serial}] Reconnected");
                });
            }
            else
            {
                _deviceLabels[serial].Foreground = Brushes.Green;
            }

            UpdateDeviceLabel(serial);
            Log($"[{serial}] {(e.IsReconnection ? "Reconnected" : "Connected")}");
        });
    }

    /// <summary>
    /// Refreshes a device's status line with its current instrument-level properties
    /// (only those that are set): battery, measurement count, mode/probe, sensor id.
    /// </summary>
    private void UpdateDeviceLabel(string serial)
    {
        if (!_deviceLabels.TryGetValue(serial, out var lbl)) return;
        if (!_instruments.TryGetValue(serial, out var inst)) return;

        var parts = new List<string>();
        if (inst.BatteryVoltage is float v)
        {
            var batt = $"{v:F2}V";
            if (inst.BatteryStatus is BatteryLevel.Low) batt += " (low)";
            else if (inst.BatteryStatus is BatteryLevel.Empty) batt += " (empty)";
            parts.Add(batt);
        }
        if (inst.MeasurementCount is int c) parts.Add($"#{c}");
        if (!string.IsNullOrEmpty(inst.Mode)) parts.Add($"Mode {inst.Mode}");
        if (!string.IsNullOrEmpty(inst.SensorId)) parts.Add($"Sensor {inst.SensorId}");

        lbl.Text = parts.Count > 0 ? $"● {serial}   {string.Join("   ", parts)}" : $"● {serial}";
    }

    private void OnMeasurement(Sdk.Models.MeasurementResult result)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");

        foreach (var (key, value) in result.Values)
        {
            var unit = result.Units.TryGetValue(key, out var u) ? u : "";
            _measurements.Insert(0, new MeasurementRow(time, result.DeviceSerial, key, value.ToString("F2"), unit));
        }

        // Keep grid manageable
        while (_measurements.Count > 200)
            _measurements.RemoveAt(_measurements.Count - 1);

        Log($"[{result.DeviceSerial}] Measurement completed ({result.Values.Count} values)");
    }

    private void Client_SensorDataReceived(object? sender, RoomSensorReadingEventArgs e)
    {
        RunOnUI(() =>
        {
            var r = e.Reading;
            var time = DateTime.Now.ToString("HH:mm:ss");
            var mac = r.MacAddress;
            if (r.Temperature.HasValue)
                _measurements.Insert(0, new MeasurementRow(time, mac, "temperature", r.Temperature.Value.ToString("F1"), "°C"));
            if (r.Humidity.HasValue)
                _measurements.Insert(0, new MeasurementRow(time, mac, "humidity", r.Humidity.Value.ToString("F1"), "%"));
            if (r.Pressure.HasValue)
                _measurements.Insert(0, new MeasurementRow(time, mac, "pressure", r.Pressure.Value.ToString("F0"), "Pa"));
        });
    }

    private void Log(string message)
    {
        var entry = $"{DateTime.Now:HH:mm:ss} {message}";
        _logEntries.Insert(0, entry);
        while (_logEntries.Count > 100)
            _logEntries.RemoveAt(_logEntries.Count - 1);
    }

    private void RunOnUI(Action action)
    {
        Dispatcher.UIThread.Post(action);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        try
        {
            _cts?.Cancel();
            var client = _client;
            _client = null;
            client?.Dispose();
            _cts?.Dispose();
            _cts = null;
        }
        catch { }
        base.OnClosing(e);
    }
}

public record MeasurementRow(string Time, string Device, string Parameter, string Value, string Unit);
