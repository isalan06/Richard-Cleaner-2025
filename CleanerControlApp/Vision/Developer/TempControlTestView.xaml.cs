using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CleanerControlApp.Modules.TempatureController.Interfaces;

namespace CleanerControlApp.Vision.Developer
{
 public partial class TempControlTestView : UserControl
 {
 private readonly ITemperatureControllers? _temperatureControllers;
 private readonly DispatcherTimer _timer;

 public TempControlTestView()
 {
 InitializeComponent();

 // Resolve ITemperatureControllers via DI from AppHost
 if (App.AppHost != null)
 {
 var svc = App.AppHost.Services.GetService(typeof(ITemperatureControllers));
 if (svc is ITemperatureControllers tcs)
 {
 _temperatureControllers = tcs;
 }
 }

 // fallback to a dummy implementation if not available
 if (_temperatureControllers == null)
 {
 _temperatureControllers = new DummyTemperatureControllers();
 }

 // timer to update RTU status periodically
 _timer = new DispatcherTimer();
 _timer.Interval = TimeSpan.FromMilliseconds(500);
 _timer.Tick += Timer_Tick;
 _timer.Start();
 }

 private void Timer_Tick(object? sender, EventArgs e)
 {
 try
 {
 bool rtuRunning = false;
 try { rtuRunning = _temperatureControllers?.ModbusRTUService?.IsRunning ?? false; } catch { }

 ellipseRtuStatus.Fill = rtuRunning ? Brushes.Green : Brushes.Red;
 txtRtuStatus.Text = rtuRunning ? "連線中" : "離線";

 bool sysRunning = false;
 try { sysRunning = _temperatureControllers?.IsRunning ?? false; } catch { }
 ellipseSystemStatus.Fill = sysRunning ? Brushes.Green : Brushes.Red;
 txtSystemStatus.Text = sysRunning ? "執行中" : "未執行";

 // Update per-device connection status (up to4)
 try
 {
 var dev = _temperatureControllers?.DeviceConnected;
 if (dev != null)
 {
 UpdateDeviceIndicator(0, dev.Length >0 ? dev[0] : false);
 UpdateDeviceIndicator(1, dev.Length >1 ? dev[1] : false);
 UpdateDeviceIndicator(2, dev.Length >2 ? dev[2] : false);
 UpdateDeviceIndicator(3, dev.Length >3 ? dev[3] : false);
 }
 else
 {
 // no info -> set all to disconnected
 UpdateDeviceIndicator(0, false);
 UpdateDeviceIndicator(1, false);
 UpdateDeviceIndicator(2, false);
 UpdateDeviceIndicator(3, false);
 }
 }
 catch { }

 // Update per-device data fields
 try
 {
 UpdateDataForIndex(0);
 UpdateDataForIndex(1);
 UpdateDataForIndex(2);
 UpdateDataForIndex(3);
 }
 catch { }
 }
 catch { }
 }

 private void UpdateDeviceIndicator(int index, bool connected)
 {
 switch (index)
 {
 case 0:
 ellipseDev0.Fill = connected ? Brushes.Green : Brushes.Red;
 txtDevStatus0.Text = connected ? "連線中" : "未連線";
 break;
 case 1:
 ellipseDev1.Fill = connected ? Brushes.Green : Brushes.Red;
 txtDevStatus1.Text = connected ? "連線中" : "未連線";
 break;
 case 2:
 ellipseDev2.Fill = connected ? Brushes.Green : Brushes.Red;
 txtDevStatus2.Text = connected ? "連線中" : "未連線";
 break;
 case 3:
 ellipseDev3.Fill = connected ? Brushes.Green : Brushes.Red;
 txtDevStatus3.Text = connected ? "連線中" : "未連線";
 break;
 }
 }

 private void UpdateDataForIndex(int index)
 {
 try
 {
 var controller = _temperatureControllers?[index];
 if (controller != null)
 {
 // SV
 var sv = controller.SV;
 // PV
 var pv = controller.PV;
 // Un
 var un = controller.Un;
 // Ctu (float)
 var ctu = controller.Ctu;
 // Status (ushort)
 var status = controller.Status;
 // AL1, AL2
 var al1 = controller.AL1;
 var al2 = controller.AL2;
 // HB (float)
 var hb = controller.HB;

 switch (index)
 {
 case 0:
 txtSV0.Text = sv.ToString();
 txtPV0.Text = pv.ToString();
 txtUn0.Text = un.ToString();
 txtCtu0.Text = ctu.ToString("F2");
 txtStatusVal0.Text = status.ToString();
 txtAL10.Text = al1.ToString();
 txtAL20.Text = al2.ToString();
 txtHB0.Text = hb.ToString("F2");
 break;
 case 1:
 txtSV1.Text = sv.ToString();
 txtPV1.Text = pv.ToString();
 txtUn1.Text = un.ToString();
 txtCtu1.Text = ctu.ToString("F2");
 txtStatusVal1.Text = status.ToString();
 txtAL11.Text = al1.ToString();
 txtAL21.Text = al2.ToString();
 txtHB1.Text = hb.ToString("F2");
 break;
 case 2:
 txtSV2.Text = sv.ToString();
 txtPV2.Text = pv.ToString();
 txtUn2.Text = un.ToString();
 txtCtu2.Text = ctu.ToString("F2");
 txtStatusVal2.Text = status.ToString();
 txtAL12.Text = al1.ToString();
 txtAL22.Text = al2.ToString();
 txtHB2.Text = hb.ToString("F2");
 break;
 case 3:
 txtSV3.Text = sv.ToString();
 txtPV3.Text = pv.ToString();
 txtUn3.Text = un.ToString();
 txtCtu3.Text = ctu.ToString("F2");
 txtStatusVal3.Text = status.ToString();
 txtAL13.Text = al1.ToString();
 txtAL23.Text = al2.ToString();
 txtHB3.Text = hb.ToString("F2");
 break;
 }
 }
 else
 {
 // controller missing -> clear values
 switch (index)
 {
 case 0:
 txtSV0.Text = "-"; txtPV0.Text = "-"; txtUn0.Text = "-"; txtCtu0.Text = "-"; txtStatusVal0.Text = "-"; txtAL10.Text = "-"; txtAL20.Text = "-"; txtHB0.Text = "-";
 break;
 case 1:
 txtSV1.Text = "-"; txtPV1.Text = "-"; txtUn1.Text = "-"; txtCtu1.Text = "-"; txtStatusVal1.Text = "-"; txtAL11.Text = "-"; txtAL21.Text = "-"; txtHB1.Text = "-";
 break;
 case 2:
 txtSV2.Text = "-"; txtPV2.Text = "-"; txtUn2.Text = "-"; txtCtu2.Text = "-"; txtStatusVal2.Text = "-"; txtAL12.Text = "-"; txtAL22.Text = "-"; txtHB2.Text = "-";
 break;
 case 3:
 txtSV3.Text = "-"; txtPV3.Text = "-"; txtUn3.Text = "-"; txtCtu3.Text = "-"; txtStatusVal3.Text = "-"; txtAL13.Text = "-"; txtAL23.Text = "-"; txtHB3.Text = "-";
 break;
 }
 }
 }
 catch { }
 }

 private async void BtnRtuOpen_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try
 {
 var svc = _temperatureControllers?.ModbusRTUService;
 if (svc != null)
 {
 bool ok = false;
 await System.Threading.Tasks.Task.Run(() => { try { ok = svc.Open(); } catch { ok = false; } });
 // immediate update
 bool running = svc.IsRunning;
 ellipseRtuStatus.Fill = running ? Brushes.Green : Brushes.Red;
 txtRtuStatus.Text = running ? "連線中" : "離線";
 }
 }
 catch { }
 }

 private void BtnRtuClose_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try
 {
 var svc = _temperatureControllers?.ModbusRTUService;
 if (svc != null)
 {
 try { svc.Close(); } catch { }
 bool running = false;
 try { running = svc.IsRunning; } catch { }
 ellipseRtuStatus.Fill = running ? Brushes.Green : Brushes.Red;
 txtRtuStatus.Text = running ? "連線中" : "離線";
 }
 }
 catch { }
 }

 private void BtnStart_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try
 {
 var tc = _temperatureControllers;
 if (tc != null)
 {
 try { tc.Start(); } catch { }
 bool sysRunning = false;
 try { sysRunning = tc.IsRunning; } catch { }
 ellipseSystemStatus.Fill = sysRunning ? Brushes.Green : Brushes.Red;
 txtSystemStatus.Text = sysRunning ? "執行中" : "未執行";
 }
 }
 catch { }
 }

 private void BtnStop_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try
 {
 var tc = _temperatureControllers;
 if (tc != null)
 {
 try { tc.Stop(); } catch { }
 bool sysRunning = false;
 try { sysRunning = tc.IsRunning; } catch { }
 ellipseSystemStatus.Fill = sysRunning ? Brushes.Green : Brushes.Red;
 txtSystemStatus.Text = sysRunning ? "執行中" : "未執行";
 }
 }
 catch { }
 }

 // Set SV handlers
 private void HandleSetSV(string text, int index)
 {
 if (string.IsNullOrWhiteSpace(text))
 {
 MessageBox.Show("請輸入數值", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
 return;
 }

 if (!int.TryParse(text.Trim(), out int val))
 {
 MessageBox.Show("輸入必須為整數", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
 return;
 }

 if (val < -999 || val >9999)
 {
 MessageBox.Show("數值範圍必須為 -999 到9999", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
 return;
 }

 try
 {
 _temperatureControllers?.SetSV(index, val);
 // update immediate display
 switch (index)
 {
 case 0: txtSV0.Text = val.ToString(); break;
 case 1: txtSV1.Text = val.ToString(); break;
 case 2: txtSV2.Text = val.ToString(); break;
 case 3: txtSV3.Text = val.ToString(); break;
 }
 }
 catch (Exception ex)
 {
 MessageBox.Show($"設定失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }

 private string GetSetSVText(int index)
 {
 var obj = this.FindName($"txtSetSV{index}") as TextBox;
 return obj?.Text ?? string.Empty;
 }

 private void BtnSetSV0_Click(object sender, RoutedEventArgs e) => HandleSetSV(GetSetSVText(0),0);
 private void BtnSetSV1_Click(object sender, RoutedEventArgs e) => HandleSetSV(GetSetSVText(1),1);
 private void BtnSetSV2_Click(object sender, RoutedEventArgs e) => HandleSetSV(GetSetSVText(2),2);
 private void BtnSetSV3_Click(object sender, RoutedEventArgs e) => HandleSetSV(GetSetSVText(3),3);

 // Simple dummy implementation to avoid null checks when DI not configured
 private class DummyTemperatureControllers : ITemperatureControllers
 {
 public bool IsRunning => false;

 public void Start() { }

 public void Stop() { }

 public bool[]? DeviceConnected => null;

 public ISingleTemperatureController? this[int index] => null;

 public int Count =>0;

 public void SetSV(int moduleIndex, int value) { }

 public CleanerControlApp.Modules.Modbus.Interfaces.IModbusRTUService? ModbusRTUService => null;
 }
 }
}
