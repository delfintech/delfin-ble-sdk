Imports System.Windows.Forms
Imports System.Drawing
Imports Delfin.Ble.Driver
Imports Delfin.Ble.Sdk
Imports Delfin.Ble.Sdk.Events
Imports Delfin.Ble.Sdk.Models

''' <summary>
''' Simple WinForms demo (VB.NET) showing how to integrate the Delfin BLE SDK.
''' Displays device connection status, live measurements, and room sensor data.
''' </summary>
Public Class MainForm
    Inherits Form

    ' UI Controls
    Private WithEvents _btnConnect As Button
    Private WithEvents _btnDisconnect As Button
    Private _txtPort As TextBox
    Private _txtDevices As TextBox
    Private _txtSensors As TextBox
    Private _lblStatus As Label
    Private _gridMeasurements As DataGridView
    Private _lstLog As ListBox
    Private _pnlDevices As FlowLayoutPanel

    ' SDK
    Private _client As IDeviceClient
    Private ReadOnly _factory As New DriverFactory()
    Private ReadOnly _deviceLabels As New Dictionary(Of String, Label)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly _instruments As New Dictionary(Of String, IInstrument)(StringComparer.OrdinalIgnoreCase)

    Public Sub New()
        Text = "Delfin BLE SDK - VB.NET WinForms Demo"
        Size = New Size(900, 700)
        StartPosition = FormStartPosition.CenterScreen
        Font = New Font("Segoe UI", 9.0F)

        ' --- Top panel: connection settings ---
        Dim pnlTop As New TableLayoutPanel With {
            .Dock = DockStyle.Top,
            .Height = 100,
            .ColumnCount = 4,
            .RowCount = 3,
            .Padding = New Padding(10)
        }
        pnlTop.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 80))
        pnlTop.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 40))
        pnlTop.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 80))
        pnlTop.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 60))

        pnlTop.Controls.Add(New Label With {.Text = "COM Port:", .Anchor = AnchorStyles.Left, .AutoSize = True}, 0, 0)
        _txtPort = New TextBox With {.Text = "COM4", .Dock = DockStyle.Fill}
        pnlTop.Controls.Add(_txtPort, 1, 0)

        pnlTop.Controls.Add(New Label With {.Text = "Devices:", .Anchor = AnchorStyles.Left, .AutoSize = True}, 2, 0)
        _txtDevices = New TextBox With {.Text = "VX10001, DCC1423", .Dock = DockStyle.Fill}
        pnlTop.Controls.Add(_txtDevices, 3, 0)

        pnlTop.Controls.Add(New Label With {.Text = "Sensors:", .Anchor = AnchorStyles.Left, .AutoSize = True}, 0, 1)
        _txtSensors = New TextBox With {.Text = "", .Dock = DockStyle.Fill}
        pnlTop.Controls.Add(_txtSensors, 1, 1)
        pnlTop.SetColumnSpan(_txtSensors, 3)

        _btnConnect = New Button With {.Text = "Connect", .Width = 80}
        pnlTop.Controls.Add(_btnConnect, 0, 2)

        _btnDisconnect = New Button With {.Text = "Disconnect", .Width = 80, .Enabled = False}
        pnlTop.Controls.Add(_btnDisconnect, 1, 2)

        _lblStatus = New Label With {.Text = "Not connected", .AutoSize = True, .ForeColor = Color.Gray}
        pnlTop.Controls.Add(_lblStatus, 2, 2)
        pnlTop.SetColumnSpan(_lblStatus, 2)

        Controls.Add(pnlTop)

        ' --- Device status panel ---
        _pnlDevices = New FlowLayoutPanel With {
            .Dock = DockStyle.Top,
            .Height = 40,
            .Padding = New Padding(10, 5, 10, 5)
        }
        Controls.Add(_pnlDevices)

        ' --- Middle: measurement grid ---
        _gridMeasurements = New DataGridView With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
        }
        _gridMeasurements.Columns.Add("Time", "Time")
        _gridMeasurements.Columns.Add("Device", "Device")
        _gridMeasurements.Columns.Add("Parameter", "Parameter")
        _gridMeasurements.Columns.Add("Value", "Value")
        _gridMeasurements.Columns.Add("Unit", "Unit")
        _gridMeasurements.Columns("Time").Width = 90
        _gridMeasurements.Columns("Device").Width = 90
        _gridMeasurements.Columns("Value").Width = 80
        _gridMeasurements.Columns("Unit").Width = 70
        Controls.Add(_gridMeasurements)

        ' --- Bottom: event log ---
        _lstLog = New ListBox With {
            .Dock = DockStyle.Bottom,
            .Height = 150,
            .Font = New Font("Consolas", 8.5F)
        }
        Controls.Add(_lstLog)

        ' Z-order
        pnlTop.BringToFront()
        _pnlDevices.BringToFront()
        _lstLog.BringToFront()
        _gridMeasurements.BringToFront()
    End Sub

    Private Async Sub _btnConnect_Click(sender As Object, e As EventArgs) Handles _btnConnect.Click
        _btnConnect.Enabled = False
        _lblStatus.Text = "Connecting..."
        _lblStatus.ForeColor = Color.Orange

        Try
            Dim port = _txtPort.Text.Trim()
            Dim devices = _txtDevices.Text.Split(","c, StringSplitOptions.RemoveEmptyEntries Or StringSplitOptions.TrimEntries).ToList()
            Dim sensors = _txtSensors.Text.Split(","c, StringSplitOptions.RemoveEmptyEntries Or StringSplitOptions.TrimEntries).ToList()

            ' Auto-detect port if empty
            If String.IsNullOrEmpty(port) Then
                Log("Detecting dongle...")
                port = Await _factory.FindPortAsync()
                If port Is Nothing Then
                    Log("ERROR: No BleuIO dongle found!")
                    _lblStatus.Text = "No dongle found"
                    _lblStatus.ForeColor = Color.Red
                    _btnConnect.Enabled = True
                    Return
                End If
                _txtPort.Text = port
                Log($"Found dongle on {port}")
            End If

            _client = _factory.CreateClient(New DriverOptions(
                ConfigFilePath:="",
                SerialPortName:=port,
                DeviceSerials:=If(devices.Count > 0, devices, Nothing),
                SensorMacs:=If(sensors.Count > 0, sensors, Nothing)
            ))

            ' Wire events
            AddHandler _client.InstrumentConnected, AddressOf Client_InstrumentConnected
            AddHandler _client.DongleLost, Sub(s, ev)
                                               Invoke(Sub()
                                                          Log("DONGLE LOST! Replug and restart.")
                                                          _lblStatus.Text = "Dongle lost!"
                                                          _lblStatus.ForeColor = Color.Red
                                                      End Sub)
                                           End Sub
            AddHandler _client.RoomSensorScanner.SensorDataReceived, AddressOf Client_SensorDataReceived

            Await _client.InitializeAsync()

            _lblStatus.Text = $"Connected ({port})"
            _lblStatus.ForeColor = Color.Green
            _btnDisconnect.Enabled = True
            Log($"Initialized on {port}. Scanning for devices...")

        Catch ex As Exception
            Log($"ERROR: {ex.Message}")
            _lblStatus.Text = "Failed"
            _lblStatus.ForeColor = Color.Red
            _btnConnect.Enabled = True
        End Try
    End Sub

    Private Async Sub _btnDisconnect_Click(sender As Object, e As EventArgs) Handles _btnDisconnect.Click
        If _client IsNot Nothing Then
            Try : Await _client.DisconnectAllAsync() : Catch : End Try
            _client.Dispose()
            _client = Nothing
        End If

        _lblStatus.Text = "Disconnected"
        _lblStatus.ForeColor = Color.Gray
        _btnConnect.Enabled = True
        _btnDisconnect.Enabled = False
        _pnlDevices.Controls.Clear()
        _deviceLabels.Clear()
        _instruments.Clear()
        Log("Disconnected.")
    End Sub

    Private Sub Client_InstrumentConnected(sender As Object, e As InstrumentConnectedEventArgs)
        Invoke(Sub()
                   Dim serial = e.Instrument.DeviceSerial
                   Dim instrument = e.Instrument
                   _instruments(serial) = instrument

                   If Not _deviceLabels.ContainsKey(serial) Then
                       Dim lbl As New Label With {
                           .Text = $"● {serial}",
                           .AutoSize = True,
                           .ForeColor = Color.Green,
                           .Font = New Font(Font, FontStyle.Bold),
                           .Margin = New Padding(5, 0, 15, 0)
                       }
                       _deviceLabels(serial) = lbl
                       _pnlDevices.Controls.Add(lbl)

                       ' Subscribe to events (once per device)
                       AddHandler instrument.MeasurementCompleted, Sub(s, result) Invoke(Sub()
                                                                                              OnMeasurement(result)
                                                                                              UpdateDeviceLabel(serial)
                                                                                          End Sub)
                       AddHandler instrument.SessionStarted, Sub(s, ev) Invoke(Sub() Log($"[{serial}] Measurement started"))
                       AddHandler instrument.BatteryLow, Sub(s, ev) Invoke(Sub() UpdateDeviceLabel(serial))
                       AddHandler instrument.BatteryEmpty, Sub(s, ev) Invoke(Sub() UpdateDeviceLabel(serial))
                       AddHandler instrument.ConnectionLost, Sub(s, ev) Invoke(Sub()
                                                                                       If _deviceLabels.ContainsKey(serial) Then _deviceLabels(serial).ForeColor = Color.Red
                                                                                       Log($"[{serial}] Connection lost")
                                                                                   End Sub)
                       AddHandler instrument.Reconnected, Sub(s, ev) Invoke(Sub()
                                                                                    If _deviceLabels.ContainsKey(serial) Then _deviceLabels(serial).ForeColor = Color.Green
                                                                                    Log($"[{serial}] Reconnected")
                                                                                End Sub)
                   Else
                       _deviceLabels(serial).ForeColor = Color.Green
                   End If

                   UpdateDeviceLabel(serial)
                   Log($"[{serial}] {If(e.IsReconnection, "Reconnected", "Connected")}")
               End Sub)
    End Sub

    ''' <summary>
    ''' Refreshes a device's status label with its current instrument-level properties
    ''' (only those that are set): battery, measurement count, mode/probe, sensor id.
    ''' </summary>
    Private Sub UpdateDeviceLabel(serial As String)
        Dim lbl As Label = Nothing
        Dim inst As IInstrument = Nothing
        If Not _deviceLabels.TryGetValue(serial, lbl) Then Return
        If Not _instruments.TryGetValue(serial, inst) Then Return

        Dim parts As New List(Of String)()
        If inst.BatteryVoltage.HasValue Then
            Dim batt = $"{inst.BatteryVoltage.Value:F2}V"
            If inst.BatteryStatus = BatteryLevel.Low Then
                batt &= " (low)"
            ElseIf inst.BatteryStatus = BatteryLevel.Empty Then
                batt &= " (empty)"
            End If
            parts.Add(batt)
        End If
        If inst.MeasurementCount.HasValue Then parts.Add($"#{inst.MeasurementCount.Value}")
        If Not String.IsNullOrEmpty(inst.Mode) Then parts.Add($"Mode {inst.Mode}")
        If Not String.IsNullOrEmpty(inst.SensorId) Then parts.Add($"Sensor {inst.SensorId}")

        lbl.Text = If(parts.Count > 0, $"● {serial}   {String.Join("   ", parts)}", $"● {serial}")
    End Sub

    Private Sub OnMeasurement(result As MeasurementResult)
        Dim time = DateTime.Now.ToString("HH:mm:ss")
        For Each kvp In result.Values
            Dim unit = ""
            result.Units.TryGetValue(kvp.Key, unit)
            _gridMeasurements.Rows.Insert(0, time, result.DeviceSerial, kvp.Key, kvp.Value.ToString("F2"), unit)
        Next

        ' Keep grid manageable
        While _gridMeasurements.Rows.Count > 200
            _gridMeasurements.Rows.RemoveAt(_gridMeasurements.Rows.Count - 1)
        End While

        Log($"[{result.DeviceSerial}] Measurement completed ({result.Values.Count} values)")
    End Sub

    Private Sub Client_SensorDataReceived(sender As Object, e As RoomSensorReadingEventArgs)
        Invoke(Sub()
                   Dim r = e.Reading
                   Dim time = DateTime.Now.ToString("HH:mm:ss")
                   Dim mac = r.MacAddress
                   If r.Temperature.HasValue Then
                       _gridMeasurements.Rows.Insert(0, time, mac, "temperature", r.Temperature.Value.ToString("F1"), "°C")
                   End If
                   If r.Humidity.HasValue Then
                       _gridMeasurements.Rows.Insert(0, time, mac, "humidity", r.Humidity.Value.ToString("F1"), "%")
                   End If
                   If r.Pressure.HasValue Then
                       _gridMeasurements.Rows.Insert(0, time, mac, "pressure", r.Pressure.Value.ToString("F0"), "Pa")
                   End If
               End Sub)
    End Sub

    Private Sub Log(message As String)
        Dim entry = $"{DateTime.Now:HH:mm:ss} {message}"
        _lstLog.Items.Insert(0, entry)
        While _lstLog.Items.Count > 100
            _lstLog.Items.RemoveAt(_lstLog.Items.Count - 1)
        End While
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        ForceCleanup()
        MyBase.OnFormClosing(e)
    End Sub

    ''' <summary>
    ''' Releases the COM port. Called on form close and process exit.
    ''' </summary>
    Public Sub ForceCleanup()
        Try
            Dim client = _client
            _client = Nothing
            client?.Dispose()
        Catch
            ' Ensure cleanup completes
        End Try
    End Sub

End Class
