using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO.Ports;
using System.Collections.Generic;
using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.Modbus.Models;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CleanerControlApp.Utilities;

namespace CleanerControlApp.Vision.Developer
{
    public partial class ModbusRTUTestView : UserControl
    {
        private readonly IModbusRTUService _modbusService;
        private readonly DispatcherTimer _timer;

        public ModbusRTUTestView()
        {
            InitializeComponent();

            // resolve service from AppHost if available
            if (App.AppHost != null)
            {
                var service = App.AppHost.Services.GetService(typeof(IModbusRTUService));
                if (service is IModbusRTUService modbusService)
                {
                    _modbusService = modbusService;
                }
            }

            // fallback dummy
            if (_modbusService == null)
            {
                _modbusService = new DummyRtuService();
            }

            // populate controls
            PopulatePortNames();
            PopulateBaudRates();
            PopulateParity();
            PopulateDataBits();
            PopulateStopBits();

            // set initial selections based on service
            cmbPortName.Text = _modbusService.PortName ?? string.Empty;
            SelectOrAddComboText(cmbBaudRate, _modbusService.BaudRate.ToString());
            SelectOrAddComboText(cmbParity, _modbusService.Parity.ToString());
            SelectOrAddComboText(cmbDataBits, _modbusService.DataBits.ToString());
            SelectOrAddComboText(cmbStopBits, _modbusService.StopBits.ToString());

            UpdateStatusIndicator();

            // timer to sync UI with service properties without interfering when user is editing
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Only update connection status periodically. Do not continuously overwrite Port/baud/parity/databits/stopbits.
                UpdateStatusIndicator();
            }
            catch { }
        }

        private void PopulatePortNames()
        {
            cmbPortName.Items.Clear();
            try
            {
                var names = SerialPort.GetPortNames().OrderBy(s => s).ToArray();
                foreach (var n in names) cmbPortName.Items.Add(n);
            }
            catch { }
        }

        private void PopulateBaudRates()
        {
            int[] rates = new int[] {110,300,600,1200,2400,4800,9600,19200,38400,57600,115200,230400 };
            cmbBaudRate.Items.Clear();
            foreach (var r in rates) cmbBaudRate.Items.Add(r.ToString());
        }

        private void PopulateParity()
        {
            cmbParity.Items.Clear();
            foreach (var p in Enum.GetValues(typeof(Parity)).Cast<Parity>()) cmbParity.Items.Add(p.ToString());
        }

        private void PopulateDataBits()
        {
            cmbDataBits.Items.Clear();
            int[] bits = new int[] {5,6,7,8 };
            foreach (var b in bits) cmbDataBits.Items.Add(b.ToString());
        }

        private void PopulateStopBits()
        {
            cmbStopBits.Items.Clear();
            foreach (var s in Enum.GetValues(typeof(StopBits)).Cast<StopBits>()) cmbStopBits.Items.Add(s.ToString());
        }

        private void SelectOrAddComboText(ComboBox cmb, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var item = cmb.Items.Cast<object>().FirstOrDefault(i => string.Equals(i.ToString(), text, StringComparison.OrdinalIgnoreCase));
            if (item != null) cmb.SelectedItem = item;
            else
            {
                cmb.Items.Add(text);
                cmb.SelectedItem = text;
            }
        }

        private void UpdateStatusIndicator()
        {
            bool open = false;
            try { open = _modbusService.IsRunning; } catch { }
            ellipseStatus.Fill = open ? Brushes.Green : Brushes.Red;
            var status = FindName("txtStatus") as TextBlock;
            if (status != null) status.Text = open ? "Connected" : "Disconnected";
        }

        // Ensure this handler exists because XAML references LostFocus="cmbPortName_LostFocus"
        private void cmbPortName_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var cmb = sender as ComboBox;
                if (cmb == null) return;
                var text = cmb.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text)) return;
                // ensure the item exists in the list so it remains selectable
                if (!cmb.Items.Cast<object>().Any(i => string.Equals(i.ToString(), text, StringComparison.OrdinalIgnoreCase)))
                {
                    cmb.Items.Add(text);
                }
            }
            catch { }
        }

        // SelectionChanged handlers no longer write directly to the service.
        private void cmbBaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // no-op: user must press 'Set' to apply changes
        }

        private void cmbParity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // no-op
        }

        private void cmbDataBits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // no-op
        }

        private void cmbStopBits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // no-op
        }

        private async void BtnSet_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // read selections and apply to service, but DO NOT open the port here.
            string port = cmbPortName.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(port)) _modbusService.PortName = port;

            if (int.TryParse(cmbBaudRate.Text, out int br)) _modbusService.BaudRate = br;
            if (Enum.TryParse<Parity>(cmbParity.Text, out var par)) _modbusService.Parity = par;
            if (int.TryParse(cmbDataBits.Text, out int db)) _modbusService.DataBits = db;
            if (Enum.TryParse<StopBits>(cmbStopBits.Text, out var sb)) _modbusService.StopBits = sb;

            // Do not attempt to open the port here. Just reflect settings in service.
            UpdateStatusIndicator();

            // If selected port not present in system list, ensure it remains in editable combo text
            try
            {
                var available = SerialPort.GetPortNames();
                if (!available.Contains(port, StringComparer.OrdinalIgnoreCase))
                {
                    if (!cmbPortName.Items.Cast<object>().Any(i => string.Equals(i.ToString(), port, StringComparison.OrdinalIgnoreCase)))
                    {
                        cmbPortName.Items.Add(port);
                    }
                }
            }
            catch { }

            await Task.Delay(10); // slight delay to allow any property change events to propagate before logging

            AppendLog($"Set applied: Port={_modbusService.PortName}, Baud={_modbusService.BaudRate}, Parity={_modbusService.Parity}, DataBits={_modbusService.DataBits}, StopBits={_modbusService.StopBits}");
        }

        // New Read button handler: read current service settings and populate controls
        private void BtnReadSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                cmbPortName.Text = _modbusService.PortName ?? string.Empty;
                SelectOrAddComboText(cmbBaudRate, _modbusService.BaudRate.ToString());
                SelectOrAddComboText(cmbParity, _modbusService.Parity.ToString());
                SelectOrAddComboText(cmbDataBits, _modbusService.DataBits.ToString());
                SelectOrAddComboText(cmbStopBits, _modbusService.StopBits.ToString());

                UpdateStatusIndicator();
                AppendLog($"Read settings: Port={_modbusService.PortName}, Baud={_modbusService.BaudRate}, Parity={_modbusService.Parity}, DataBits={_modbusService.DataBits}, StopBits={_modbusService.StopBits}");
            }
            catch (Exception ex)
            {
                AppendLog($"Read settings failed: {ex.Message}");
            }
        }

        private async void BtnConnect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AppendLog("Connecting...");
            bool ok = false;
            try
            {
                ok = await Task.Run(() => _modbusService.Open());
            }
            catch (Exception ex)
            {
                AppendLog($"Connect failed: {ex.Message}");
            }
            UpdateStatusIndicator();
            AppendLog(ok ? "Connected" : "Connect failed");
        }

        private async void BtnDisconnect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AppendLog("Disconnecting...");
            await Task.Run(() => { try { _modbusService.Close(); } catch { } });
            UpdateStatusIndicator();
            AppendLog("Disconnected");
        }

        // Read buttons
        private void BtnReadCoils_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExecuteRead(ModbusFunctionCode.ReadCoils);
        }

        private void BtnReadDiscreteInputs_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExecuteRead(ModbusFunctionCode.ReadDiscreteInputs);
        }

        private void BtnReadHoldingRegisters_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExecuteRead(ModbusFunctionCode.ReadHoldingRegisters);
        }

        private void BtnReadInputRegisters_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExecuteRead(ModbusFunctionCode.ReadInputRegisters);
        }

        private async void ExecuteRead(ModbusFunctionCode function)
        {
            if (!TryGetSlaveStartNumber(out byte slave, out ushort start, out ushort number, out string err))
            {
                AppendLog(err);
                return;
            }

            var frame = new ModbusRTUFrame();
            // choose overload based on function: coils/discrete use bool[], registers use ushort[]
            if (function == ModbusFunctionCode.ReadCoils || function == ModbusFunctionCode.ReadDiscreteInputs)
            {
                frame.Set(slave, (byte)function, start, number, (bool[]?)null);
            }
            else
            {
                frame.Set(slave, (byte)function, start, number, (ushort[]?)null);
            }

            // ensure open
            if (!_modbusService.IsRunning)
            {
                try { _modbusService.Open(); } catch { }
            }

            AppendLog($"Executing {function} Slave={slave} Start={start} Count={number}...");

            var res = await _modbusService.Act(frame);
            if (res == null)
            {
                AppendLog($"Execute {function} => null");
                return;
            }

            // populate dgResults for ushort data
            var dg = FindName("dgResults") as DataGrid;
            if (dg != null)
            {
                var rows = new List<object>();
                if (res.Data != null && res.Data.Length >0)
                {
                    for (int i =0; i < res.Data.Length; i++) rows.Add(new { Index = i, Address = (res.StartAddress + i).ToString(), Value = res.Data[i].ToString() });
                }
                else if (res.BoolData != null && res.BoolData.Length >0)
                {
                    for (int i =0; i < res.BoolData.Length; i++) rows.Add(new { Index = i, Address = (res.StartAddress + i).ToString(), Value = res.BoolData[i].ToString() });
                }
                else
                {
                    rows.Add(new { Index =0, Address = res.StartAddress.ToString(), Value = "No data" });
                }

                dg.ItemsSource = rows;
            }

            // also write a short log
            if (res.Data != null && res.Data.Length >0)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Read {res.Data.Length} registers starting at {res.StartAddress}:");
                for (int i =0; i < res.Data.Length; i++) sb.AppendLine($"{res.StartAddress + i} = {res.Data[i]}");
                AppendLog(sb.ToString());
            }
            else if (res.BoolData != null && res.BoolData.Length >0)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Read {res.BoolData.Length} bits starting at {res.StartAddress}:");
                for (int i =0; i < res.BoolData.Length; i++) sb.AppendLine($"{res.StartAddress + i} = {res.BoolData[i]}");
                AppendLog(sb.ToString());
            }
            else
            {
                AppendLog("No data");
            }
        }

        private void BtnWriteCoils_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!TryGetSlaveStartNumber(out byte slave, out ushort start, out ushort number, out string err))
            {
                AppendLog(err);
                return;
            }

            // parse up to4 boolean values
            var bools = new List<bool>();
            for (int i =0; i <4; i++)
            {
                var tb = FindName($"txtSetVal{i}") as TextBox;
                string txt = tb?.Text?.Trim() ?? string.Empty;
                if (TryParseBoolLike(txt, out bool b)) bools.Add(b);
                else break;
            }

            if (bools.Count ==0)
            {
                AppendLog("No coil values provided.");
                return;
            }

            ushort writeCount = (ushort)Math.Min(number, (ushort)bools.Count);
            if (writeCount ==1)
            {
                var frame = new ModbusRTUFrame();
                frame.Set(slave,0x05, start,1, new bool[] { bools[0] });
                Task.Run(async () =>
                {
                    var res = await _modbusService.Act(frame);
                    Dispatcher.Invoke(() => { AppendLog(res != null ? "Write single coil OK" : "Write failed"); });
                });
            }
            else
            {
                var frame = new ModbusRTUFrame();
                frame.Set(slave,0x0F, start, writeCount, bools.Take(writeCount).ToArray());
                Task.Run(async () =>
                {
                    var res = await _modbusService.Act(frame);
                    Dispatcher.Invoke(() => { AppendLog(res != null ? "Write multiple coils OK" : "Write failed"); });
                });
            }
        }

        private void BtnWriteHoldingRegisters_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!TryGetSlaveStartNumber(out byte slave, out ushort start, out ushort number, out string err))
            {
                AppendLog(err);
                return;
            }

            // parse up to4 values
            var vals = new List<ushort>();
            for (int i =0; i <4; i++)
            {
                var tb = FindName($"txtSetVal{i}") as TextBox;
                string txt = tb?.Text?.Trim() ?? string.Empty;
                if (ushort.TryParse(txt, out ushort v)) vals.Add(v);
                else break;
            }

            if (vals.Count ==0)
            {
                AppendLog("No register values provided.");
                return;
            }

            ushort writeCount = (ushort)Math.Min(number, (ushort)vals.Count);
            if (writeCount ==1)
            {
                var frame = new ModbusRTUFrame();
                frame.Set(slave,0x06, start,1, new ushort[] { vals[0] });
                Task.Run(async () =>
                {
                    var res = await _modbusService.Act(frame);
                    Dispatcher.Invoke(() => { AppendLog(res != null ? "Write single register OK" : "Write failed"); });
                });
            }
            else
            {
                var frame = new ModbusRTUFrame();
                frame.Set(slave,0x10, start, writeCount, vals.Take(writeCount).ToArray());
                Task.Run(async () =>
                {
                    var res = await _modbusService.Act(frame);
                    Dispatcher.Invoke(() => { AppendLog(res != null ? "Write multiple registers OK" : "Write failed"); });
                });
            }
        }

        // parse slave/start/number with basic validation
        private bool TryGetSlaveStartNumber(out byte slave, out ushort start, out ushort number, out string error)
        {
            slave =1; start =0; number =1; error = string.Empty;
            var tbSlave = FindName("txtSlaveId") as TextBox;
            var tbStart = FindName("txtStartAddress") as TextBox;
            var tbNum = FindName("txtNumber") as TextBox;

            if (tbSlave == null || !byte.TryParse(tbSlave.Text.Trim(), out slave)) { error = "Invalid Slave ID (0-255)."; return false; }
            if (tbStart == null || !ushort.TryParse(tbStart.Text.Trim(), out start)) { error = "Invalid Start Address (0-65535)."; return false; }
            if (tbNum == null || !ushort.TryParse(tbNum.Text.Trim(), out number) || number ==0) { error = "Invalid Number (must be >0)."; return false; }
            return true;
        }

        private bool TryParseBoolLike(string txt, out bool value)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(txt)) return false;
            var t = txt.Trim().ToLowerInvariant();
            if (t == "1" || t == "true") { value = true; return true; }
            if (t == "0" || t == "false") { value = false; return true; }
            return false;
        }

        private void AppendLog(string text)
        {
            try
            {
                var log = FindName("txtLog") as TextBox;
                if (log == null) return;
                log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\n");
                log.ScrollToEnd();
            }
            catch { }
        }

        #region Dummy fallback
        private class DummyRtuService : IModbusRTUService
        {
            public string PortName { get; set; } = "COM1";
            public int BaudRate { get; set; } =9600;
            public Parity Parity { get; set; } = Parity.None;
            public int DataBits { get; set; } =8;
            public StopBits StopBits { get; set; } = StopBits.One;
            public bool IsRunning { get; private set; } = false;
            public int Timeout { get; set; } =1000;

            public bool Open()
            {
                IsRunning = true;
                return true;
            }

            public void Close()
            {
                IsRunning = false;
            }

            public Task<ModbusRTUFrame?> Act(ModbusRTUFrame? coammand)
            {
                if (coammand == null) return Task.FromResult<ModbusRTUFrame?>(null);
                var f = new ModbusRTUFrame(coammand);
                f.HasResponse = true;
                if (f.IsRead)
                {
                    if (f.FunctionCode ==0x1 || f.FunctionCode ==0x2)
                    {
                        f.BoolData = new bool[f.DataNumber];
                        for (int i =0; i < f.DataNumber; i++) f.BoolData[i] = (i %2) ==0;
                    }
                    else
                    {
                        f.Data = new ushort[f.DataNumber];
                        for (int i =0; i < f.Data.Length; i++) f.Data[i] = (ushort)(f.StartAddress + i);
                    }
                }
                return Task.FromResult<ModbusRTUFrame?>(f);
            }

            public void RefreshSerialPortSettings(CommunicationSettings? settings)
            {
                // Dummy implementation: do nothing
            }
        }
        #endregion
    }
}
