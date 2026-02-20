using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.Modbus.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanerControlApp.Vision.Developer
{
 /// <summary>
 /// Interaction logic for ModbusTestView.xaml
 /// </summary>
 public partial class ModbusTestView : UserControl
 {
 private readonly IModbusTCPService _modbusService;
 private readonly DispatcherTimer _timer;

 public ModbusTestView()
 {
 InitializeComponent();

 // resolve service from AppHost if available
 if (App.AppHost != null)
 {
 var service = App.AppHost.Services.GetService(typeof(IModbusTCPService));
 if (service is IModbusTCPService modbusService)
 {
 _modbusService = modbusService;
 }
 }

 // fallback if not available
 if (_modbusService == null)
 {
 // create a simple default implementation to avoid null refs
 _modbusService = new DummyModbusService();
 }

 // initialize fields (use FindName to avoid generated-field dependency)
 var lblIp = FindName("lblIpValue") as Label;
 var lblPort = FindName("lblPortValue") as Label;
 var txtIpBox = FindName("txtIp") as TextBox;
 var txtPortBox = FindName("txtPort") as TextBox;

 if (lblIp != null) lblIp.Content = _modbusService.Ip;
 if (lblPort != null) lblPort.Content = _modbusService.Port.ToString();
 if (txtIpBox != null) txtIpBox.Text = _modbusService.Ip;
 if (txtPortBox != null) txtPortBox.Text = _modbusService.Port.ToString();

 // setup timer to update status periodically
 _timer = new DispatcherTimer();
 _timer.Interval = TimeSpan.FromMilliseconds(500);
 _timer.Tick += Timer_Tick;
 _timer.Start();
 }

 private void Timer_Tick(object? sender, EventArgs e)
 {
 var txtStatusBox = FindName("txtStatus") as TextBlock;
 var ellipse = FindName("ellipseStatus") as System.Windows.Shapes.Ellipse;
 var lblIp = FindName("lblIpValue") as Label;
 var lblPort = FindName("lblPortValue") as Label;

 bool connected = _modbusService.IsConnected;
 if (txtStatusBox != null) txtStatusBox.Text = connected ? "Connected" : "Disconnected";
 if (ellipse != null) ellipse.Fill = connected ? Brushes.Green : Brushes.Red;

 // keep labels in sync with service properties
 if (lblIp != null) lblIp.Content = _modbusService.Ip;
 if (lblPort != null) lblPort.Content = _modbusService.Port.ToString();
 }

 private void BtnSet_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 var txtIpBox = FindName("txtIp") as TextBox;
 var txtPortBox = FindName("txtPort") as TextBox;
 var lblIp = FindName("lblIpValue") as Label;
 var lblPort = FindName("lblPortValue") as Label;

 // set IP and Port from textboxes
 if (txtIpBox != null && !string.IsNullOrWhiteSpace(txtIpBox.Text))
 {
 _modbusService.Ip = txtIpBox.Text.Trim();
 }

 if (txtPortBox != null && int.TryParse(txtPortBox.Text.Trim(), out int p))
 {
 _modbusService.Port = p;
 }

 // update labels
 if (lblIp != null) lblIp.Content = _modbusService.Ip;
 if (lblPort != null) lblPort.Content = _modbusService.Port.ToString();
 }

 private async void BtnConnect_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 var btnConnect = FindName("btnConnect") as Button;
 var btnDisconnect = FindName("btnDisconnect") as Button;
 var txtStatusBox = FindName("txtStatus") as TextBlock;
 var ellipse = FindName("ellipseStatus") as System.Windows.Shapes.Ellipse;
 var log = FindName("txtLog") as TextBox;
 
 if (btnConnect != null) btnConnect.IsEnabled = false;
 if (btnDisconnect != null) btnDisconnect.IsEnabled = false;
 
 var addr = $"{_modbusService.Ip}:{_modbusService.Port}";
 if (txtStatusBox != null) txtStatusBox.Text = $"Connecting {addr}...";
 if (ellipse != null) ellipse.Fill = Brushes.Goldenrod;
 log?.AppendText($"[{DateTime.Now:HH:mm:ss}] Connecting to {addr}...\n");
 
 bool ok = await Task.Run(() => _modbusService.Connect());
 
 if (ok)
 {
 if (txtStatusBox != null) txtStatusBox.Text = $"Connected {addr}";
 if (ellipse != null) ellipse.Fill = Brushes.Green;
 log?.AppendText($"[{DateTime.Now:HH:mm:ss}] Connected to {addr}\n");
 }
 else
 {
 if (txtStatusBox != null) txtStatusBox.Text = "Disconnected";
 if (ellipse != null) ellipse.Fill = Brushes.Red;
 log?.AppendText($"[{DateTime.Now:HH:mm:ss}] Connect failed to {addr}\n");
 }
 
 if (btnConnect != null) btnConnect.IsEnabled = true;
 if (btnDisconnect != null) btnDisconnect.IsEnabled = true;
 log?.ScrollToEnd();
 }

 private async void BtnDisconnect_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 var btnConnect = FindName("btnConnect") as Button;
 var btnDisconnect = FindName("btnDisconnect") as Button;
 var txtStatusBox = FindName("txtStatus") as TextBlock;
 var ellipse = FindName("ellipseStatus") as System.Windows.Shapes.Ellipse;
 var log = FindName("txtLog") as TextBox;
 
 if (btnConnect != null) btnConnect.IsEnabled = false;
 if (btnDisconnect != null) btnDisconnect.IsEnabled = false;
 
 var addr = $"{_modbusService.Ip}:{_modbusService.Port}";
 if (txtStatusBox != null) txtStatusBox.Text = $"Disconnecting {addr}...";
 if (ellipse != null) ellipse.Fill = Brushes.Goldenrod;
 log?.AppendText($"[{DateTime.Now:HH:mm:ss}] Disconnecting from {addr}...\n");
 
 bool ok = await Task.Run(() => _modbusService.Disconnect());
 
 if (ok)
 {
 if (txtStatusBox != null) txtStatusBox.Text = $"Disconnected {addr}";
 if (ellipse != null) ellipse.Fill = Brushes.Red;
 log?.AppendText($"[{DateTime.Now:HH:mm:ss}] Disconnected from {addr}\n");
 }
 else
 {
 if (txtStatusBox != null) txtStatusBox.Text = "Disconnect failed";
 if (ellipse != null) ellipse.Fill = Brushes.OrangeRed;
 log?.AppendText($"[{DateTime.Now:HH:mm:ss}] Disconnect failed from {addr}\n");
 }

 if (btnConnect != null) btnConnect.IsEnabled = true;
 if (btnDisconnect != null) btnDisconnect.IsEnabled = true;
 log?.ScrollToEnd();
 }

 // Read button handlers: build ModbusTCPFrame, assign to service and execute
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

 private void BtnWriteCoils_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 var errorBlock = FindName("txtSetValueError") as TextBlock;
 if (errorBlock != null) { errorBlock.Visibility = System.Windows.Visibility.Collapsed; errorBlock.Text = string.Empty; }

 if (!TryGetSlaveStartNumber(out byte slave, out ushort start, out ushort number, out string err))
 {
 if (errorBlock != null) { errorBlock.Text = err; errorBlock.Visibility = System.Windows.Visibility.Visible; }
 return;
 }

 // parse up to4 set values as bool
 var vals = new List<bool>();
 for (int i =0; i <4; i++)
 {
 var tb = FindName($"txtSetVal{i}") as TextBox;
 string txt = tb?.Text?.Trim() ?? string.Empty;
 if (TryParseBoolLike(txt, out bool b)) vals.Add(b);
 else
 {
 if (errorBlock != null) { errorBlock.Text = $"Invalid coil value at position {i}: '{txt}'. Use true/false or1/0."; errorBlock.Visibility = System.Windows.Visibility.Visible; }
 return;
 }
 }

 ushort writeCount = (ushort)Math.Min(number, (ushort)4);
 if (number >4)
 {
 var log = FindName("txtLog") as TextBox;
 log?.AppendText($"[{DateTime.Now:HH:mm:ss}] Warning: Number ({number}) >4, only first4 values will be written.\n");
 }

 var frame = new ModbusTCPFrame();
 frame.Set(slave, (byte)ModbusFunctionCode.WriteMultipleCoils, start, writeCount, null);
 frame.BoolData = vals.Take(writeCount).ToArray();

 // Execute
 if (!_modbusService.IsConnected) _modbusService.Connect();
 _modbusService.ExecuteFrame = frame;
 bool ok = _modbusService.Execute();

 // Update UI
 var logBox = FindName("txtLog") as TextBox;
 var dg = FindName("dgResults") as DataGrid;
 if (logBox != null) { logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Write Coils => {ok}\n"); logBox.ScrollToEnd(); }
 if (dg != null)
 {
 var rows = new List<object>();
 for (int i =0; i < frame.BoolData.Length; i++) rows.Add(new { Index = i, Address = (frame.StartAddress + i).ToString(), Value = frame.BoolData[i].ToString() });
 dg.ItemsSource = rows;
 }
 }

 private void BtnWriteHoldingRegisters_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 var errorBlock = FindName("txtSetValueError") as TextBlock;
 if (errorBlock != null) { errorBlock.Visibility = System.Windows.Visibility.Collapsed; errorBlock.Text = string.Empty; }

 if (!TryGetSlaveStartNumber(out byte slave, out ushort start, out ushort number, out string err))
 {
 if (errorBlock != null) { errorBlock.Text = err; errorBlock.Visibility = System.Windows.Visibility.Visible; }
 return;
 }

 // parse up to4 set values as ushort
 var vals = new List<ushort>();
 for (int i =0; i <4; i++)
 {
 var tb = FindName($"txtSetVal{i}") as TextBox;
 string txt = tb?.Text?.Trim() ?? string.Empty;
 if (ushort.TryParse(txt, out ushort v)) vals.Add(v);
 else
 {
 if (errorBlock != null) { errorBlock.Text = $"Invalid register value at position {i}: '{txt}'. Enter0..65535."; errorBlock.Visibility = System.Windows.Visibility.Visible; }
 return;
 }
 }

 ushort writeCount = (ushort)Math.Min(number, (ushort)4);
 if (number >4)
 {
 var log = FindName("txtLog") as TextBox;
 log?.AppendText($"[{DateTime.Now:HH:mm:ss}] Warning: Number ({number}) >4, only first4 values will be written.\n");
 }

 var frame = new ModbusTCPFrame();
 frame.Set(slave, (byte)ModbusFunctionCode.WriteMultipleRegisters, start, writeCount, vals.Take(writeCount).ToArray());

 // Execute
 if (!_modbusService.IsConnected) _modbusService.Connect();
 _modbusService.ExecuteFrame = frame;
 bool ok = _modbusService.Execute();

 // Update UI
 var logBox = FindName("txtLog") as TextBox;
 var dg = FindName("dgResults") as DataGrid;
 if (logBox != null) { logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Write HR => {ok}\n"); logBox.ScrollToEnd(); }
 if (dg != null)
 {
 var rows = new List<object>();
 for (int i =0; i < frame.Data?.Length; i++) rows.Add(new { Index = i, Address = (frame.StartAddress + i).ToString(), Value = frame.Data[i].ToString() });
 dg.ItemsSource = rows;
 }
 }

 private void ExecuteRead(ModbusFunctionCode function)
 {
 // build frame from inputs
 var frame = BuildFrame(function);

 // display current command
 var txtCmd = FindName("txtCurrentCommand") as TextBlock;
 if (txtCmd != null)
 {
 txtCmd.Text = function.ToString();
 }

 // ensure service connected
 if (!_modbusService.IsConnected)
 {
 _modbusService.Connect();
 }

 // assign frame and execute
 _modbusService.ExecuteFrame = frame;
 bool ok = _modbusService.Execute();

 // show result/log
 var dg = FindName("dgResults") as DataGrid;
 var log = FindName("txtLog") as TextBox;

 if (log != null)
 {
 log.AppendText($"[{DateTime.Now:HH:mm:ss}] Execute {function} => {ok}\n");
 log.ScrollToEnd();
 }

 if (dg != null)
 {
 dg.ItemsSource = null;
 var rows = new List<object>();

 var ef = _modbusService.ExecuteFrame;
 if (ef != null)
 {
 if (ef.BoolData != null && ef.BoolData.Length >0)
 {
 for (int i =0; i < ef.BoolData.Length; i++)
 {
 rows.Add(new { Index = i, Address = (ef.StartAddress + i).ToString(), Value = ef.BoolData[i].ToString() });
 }
 }
 else if (ef.Data != null && ef.Data.Length >0)
 {
 for (int i =0; i < ef.Data.Length; i++)
 {
 rows.Add(new { Index = i, Address = (ef.StartAddress + i).ToString(), Value = ef.Data[i].ToString() });
 }
 }
 else
 {
 rows.Add(new { Index =0, Address = ef.StartAddress.ToString(), Value = "No data" });
 }
 dg.ItemsSource = rows;
 }
 }
 }

 private ModbusTCPFrame BuildFrame(ModbusFunctionCode func)
 {
 // parse inputs with safe defaults using FindName
 byte slave =1;
 var tbSlave = FindName("txtSlaveId") as TextBox;
 if (tbSlave != null && byte.TryParse(tbSlave.Text, out byte s)) slave = s;

 ushort start =0;
 var tbStart = FindName("txtStartAddress") as TextBox;
 if (tbStart != null && ushort.TryParse(tbStart.Text, out ushort st)) start = st;

 ushort number =1;
 var tbNumber = FindName("txtNumber") as TextBox;
 if (tbNumber != null && ushort.TryParse(tbNumber.Text, out ushort n)) number = n;

 var frame = new ModbusTCPFrame();
 frame.Set(slave, (byte)func, start, number, null);
 return frame;
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

 // accepts true/false or1/0
 private bool TryParseBoolLike(string txt, out bool value)
 {
 value = false;
 if (string.IsNullOrWhiteSpace(txt)) return false;
 var t = txt.Trim().ToLowerInvariant();
 if (t == "1" || t == "true") { value = true; return true; }
 if (t == "0" || t == "false") { value = false; return true; }
 return false;
 }

 #region Dummy service fallback
 private class DummyModbusService : IModbusTCPService
 {
 public string Ip { get; set; } = "127.0.0.1";
 public int Port { get; set; } = 502;
 public bool IsConnected { get; private set; } = false;

 public bool Connect() { IsConnected = true; return true; }
 public bool Disconnect() { IsConnected = false; return true; }

 public Task<bool> ConnectAsync() => Task.FromResult(Connect());
 public Task<bool> DisconnectAsync() => Task.FromResult(Disconnect());

 public Modules.Modbus.Models.ModbusTCPFrame ExecuteFrame { get; set; } = new Modules.Modbus.Models.ModbusTCPFrame();

 public bool Execute()
 {
 // no-op: fill ExecuteFrame with fake data
 if (ExecuteFrame.FunctionCodeName == ModbusFunctionCode.ReadCoils || ExecuteFrame.FunctionCodeName == ModbusFunctionCode.ReadDiscreteInputs)
 {
 ExecuteFrame.BoolData = new bool[ExecuteFrame.DataNumber];
 for (int i =0; i < ExecuteFrame.BoolData.Length; i++) ExecuteFrame.BoolData[i] = (i %2) ==0;
 }
 else
 {
 ExecuteFrame.Data = new ushort[ExecuteFrame.DataNumber];
 for (int i =0; i < ExecuteFrame.Data.Length; i++) ExecuteFrame.Data[i] = (ushort)(i + ExecuteFrame.StartAddress);
 }
 return true;
 }

 public Task<Modules.Modbus.Models.ModbusTCPFrame?> ExecuteAsync(Modules.Modbus.Models.ModbusTCPFrame frame)
 {
 ExecuteFrame = frame;
 Execute();
 return Task.FromResult<Modules.Modbus.Models.ModbusTCPFrame?>(ExecuteFrame);
 }
 }
 #endregion
 }
}
