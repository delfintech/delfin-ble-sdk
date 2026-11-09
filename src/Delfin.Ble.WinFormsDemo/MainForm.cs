using Delfin.Ble.Driver;
using Delfin.Ble.Sdk;
using Delfin.Ble.Sdk.Events;
using Delfin.Ble.Sdk.Models;

namespace Delfin.Ble.WinFormsDemo;

/// <summary>
/// Simple WinForms demo showing how to integrate the Delfin BLE SDK.
/// Displays device connection status, live measurements, and room sensor data.
/// </summary>
public class MainForm : Form
{
    // UI Controls
    private readonly Button _btnConnect;
    private readonly Button _btnDisconnect;
    private readonly TextBox _txtPort;
    private readonly TextBox _txtDevices;
    private readonly TextBox _txtSensors;
    private readonly Label _lblStatus;
    private readonly DataGridView _gridMeasurements;
    private readonly ListBox _lstLog;
    private readonly Panel _pnlDevices;

    // SDK
    private IDeviceClient? _client;
    private readonly DriverFactory _factory = new();
    private readonly Dictionary<string, Label> _deviceLabels = new();
    private readonly Dictionary<string, IInstrument> _instruments = new();

    public MainForm()
    {
        Text = "Delfin BLE SDK - WinForms Demo";
        Size = new Size(900, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        // --- Top panel: connection settings ---
        var pnlTop = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 100,
            ColumnCount = 4,
            RowCount = 3,
            Padding = new Padding(10)
        };
        pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        pnlTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

        pnlTop.Controls.Add(new Label { Text = "COM Port:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 0);
        _txtPort = new TextBox { Text = "COM4", Dock = DockStyle.Fill };
        pnlTop.Controls.Add(_txtPort, 1, 0);

        pnlTop.Controls.Add(new Label { Text = "Devices:", Anchor = AnchorStyles.Left, AutoSize = true }, 2, 0);
        _txtDevices = new TextBox { Text = "VX10001, DCC1423", Dock = DockStyle.Fill };
        pnlTop.Controls.Add(_txtDevices, 3, 0);

        pnlTop.Controls.Add(new Label { Text = "Sensors:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 1);
        _txtSensors = new TextBox { Text = "", Dock = DockStyle.Fill, PlaceholderText = "C7:96:9A:4A:A5:1F (optional)" };
        pnlTop.Controls.Add(_txtSensors, 1, 1);
        pnlTop.SetColumnSpan(_txtSensors, 3);

        _btnConnect = new Button { Text = "Connect", Width = 80 };
        _btnConnect.Click += BtnConnect_Click;
        pnlTop.Controls.Add(_btnConnect, 0, 2);

        _btnDisconnect = new Button { Text = "Disconnect", Width = 80, Enabled = false };
        _btnDisconnect.Click += BtnDisconnect_Click;
        pnlTop.Controls.Add(_btnDisconnect, 1, 2);

        _lblStatus = new Label { Text = "Not connected", AutoSize = true, ForeColor = Color.Gray };
        pnlTop.Controls.Add(_lblStatus, 2, 2);
        pnlTop.SetColumnSpan(_lblStatus, 2);

        Controls.Add(pnlTop);

        // --- Device status panel ---
        _pnlDevices = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(10, 5, 10, 5),
            AutoScroll = true
        };
        Controls.Add(_pnlDevices);

        // --- Middle: measurement grid ---
        _gridMeasurements = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        _gridMeasurements.Columns.Add("Time", "Time");
        _gridMeasurements.Columns.Add("Device", "Device");
        _gridMeasurements.Columns.Add("Parameter", "Parameter");
        _gridMeasurements.Columns.Add("Value", "Value");
        _gridMeasurements.Columns.Add("Unit", "Unit");
        _gridMeasurements.Columns["Time"]!.Width = 90;
        _gridMeasurements.Columns["Device"]!.Width = 90;
        _gridMeasurements.Columns["Value"]!.Width = 80;
        _gridMeasurements.Columns["Unit"]!.Width = 70;

        Controls.Add(_gridMeasurements);

        // --- Bottom: event log ---
        _lstLog = new ListBox
        {
            Dock = DockStyle.Bottom,
            Height = 150,
            Font = new Font("Consolas", 8.5f)
        };
        Controls.Add(_lstLog);

        // Ensure correct Z-order (top to bottom)
        pnlTop.BringToFront();
        _pnlDevices.BringToFront();
        _lstLog.BringToFront();
        _gridMeasurements.BringToFront();
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        _btnConnect.Enabled = false;
        _lblStatus.Text = "Connecting...";
        _lblStatus.ForeColor = Color.Orange;

        try
        {
            var port = _txtPort.Text.Trim();
            var devices = _txtDevices.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            var sensors = _txtSensors.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            // Auto-detect port if empty
            if (string.IsNullOrEmpty(port))
            {
                Log("Detecting dongle...");
                port = await _factory.FindPortAsync();
                if (port == null)
                {
                    Log("ERROR: No BleuIO dongle found!");
                    _lblStatus.Text = "No dongle found";
                    _lblStatus.ForeColor = Color.Red;
                    _btnConnect.Enabled = true;
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

            // Wire events
            _client.InstrumentConnected += Client_InstrumentConnected;
            _client.DongleLost += (_, _) => Invoke(() =>
            {
                Log("DONGLE LOST! Replug and restart.");
                _lblStatus.Text = "Dongle lost!";
                _lblStatus.ForeColor = Color.Red;
            });
            _client.RoomSensorScanner.SensorDataReceived += Client_SensorDataReceived;

            await _client.InitializeAsync();

            _lblStatus.Text = $"Connected ({port})";
            _lblStatus.ForeColor = Color.Green;
            _btnDisconnect.Enabled = true;
            Log($"Initialized on {port}. Scanning for devices...");
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            _lblStatus.Text = "Failed";
            _lblStatus.ForeColor = Color.Red;
            _btnConnect.Enabled = true;
        }
    }

    private async void BtnDisconnect_Click(object? sender, EventArgs e)
    {
        if (_client != null)
        {
            try { await _client.DisconnectAllAsync(); } catch { }
            _client.Dispose();
            _client = null;
        }

        _lblStatus.Text = "Disconnected";
        _lblStatus.ForeColor = Color.Gray;
        _btnConnect.Enabled = true;
        _btnDisconnect.Enabled = false;
        _pnlDevices.Controls.Clear();
        _deviceLabels.Clear();
        _instruments.Clear();
        Log("Disconnected.");
    }

    private void Client_InstrumentConnected(object? sender, InstrumentConnectedEventArgs e)
    {
        Invoke(() =>
        {
            var serial = e.Instrument.DeviceSerial;
            var instrument = e.Instrument;
            _instruments[serial] = instrument;

            // Add/update device status label
            if (!_deviceLabels.ContainsKey(serial))
            {
                var lbl = new Label
                {
                    Text = $"● {serial}",
                    AutoSize = true,
                    ForeColor = Color.Green,
                    Font = new Font(Font, FontStyle.Bold),
                    Margin = new Padding(5, 0, 15, 0)
                };
                _deviceLabels[serial] = lbl;
                _pnlDevices.Controls.Add(lbl);

                // Subscribe to measurement events (only once per device)
                instrument.MeasurementCompleted += (_, result) => Invoke(() => { OnMeasurement(result); UpdateDeviceLabel(serial); });
                instrument.SessionStarted += (_, ev) => Invoke(() => Log($"[{serial}] Measurement started (session: {ev.SessionId:N})"));
                instrument.BatteryLow += (_, _) => Invoke(() => UpdateDeviceLabel(serial));
                instrument.BatteryEmpty += (_, _) => Invoke(() => UpdateDeviceLabel(serial));
                instrument.ConnectionLost += (_, _) => Invoke(() =>
                {
                    if (_deviceLabels.TryGetValue(serial, out var l)) l.ForeColor = Color.Red;
                    Log($"[{serial}] Connection lost");
                });
                instrument.Reconnected += (_, _) => Invoke(() =>
                {
                    if (_deviceLabels.TryGetValue(serial, out var l)) l.ForeColor = Color.Green;
                    Log($"[{serial}] Reconnected");
                });
            }
            else
            {
                _deviceLabels[serial].ForeColor = Color.Green;
            }

            UpdateDeviceLabel(serial);
            Log($"[{serial}] {(e.IsReconnection ? "Reconnected" : "Connected")}");
        });
    }

    /// <summary>
    /// Refreshes a device's status label with its current instrument-level properties
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
            _gridMeasurements.Rows.Insert(0, time, result.DeviceSerial, key, value.ToString("F2"), unit);
        }

        // Keep grid manageable
        while (_gridMeasurements.Rows.Count > 200)
            _gridMeasurements.Rows.RemoveAt(_gridMeasurements.Rows.Count - 1);

        Log($"[{result.DeviceSerial}] Measurement completed ({result.Values.Count} values)");
    }

    private void Client_SensorDataReceived(object? sender, RoomSensorReadingEventArgs e)
    {
        Invoke(() =>
        {
            var r = e.Reading;
            var time = DateTime.Now.ToString("HH:mm:ss");
            var mac = r.MacAddress;
            if (r.Temperature.HasValue)
                _gridMeasurements.Rows.Insert(0, time, mac, "temperature", r.Temperature.Value.ToString("F1"), "°C");
            if (r.Humidity.HasValue)
                _gridMeasurements.Rows.Insert(0, time, mac, "humidity", r.Humidity.Value.ToString("F1"), "%");
            if (r.Pressure.HasValue)
                _gridMeasurements.Rows.Insert(0, time, mac, "pressure", r.Pressure.Value.ToString("F0"), "Pa");
        });
    }

    private void Log(string message)
    {
        var entry = $"{DateTime.Now:HH:mm:ss} {message}";
        _lstLog.Items.Insert(0, entry);
        while (_lstLog.Items.Count > 100)
            _lstLog.Items.RemoveAt(_lstLog.Items.Count - 1);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        ForceCleanup();
        base.OnFormClosing(e);
    }

    /// <summary>
    /// Releases the COM port and disposes the client.
    /// Called from OnFormClosing and ProcessExit to ensure cleanup on any shutdown.
    /// </summary>
    public void ForceCleanup()
    {
        try
        {
            var client = _client;
            _client = null;
            client?.Dispose(); // Synchronously closes serial port
        }
        catch
        {
            // Ensure cleanup completes even if dispose fails
        }
    }
}
