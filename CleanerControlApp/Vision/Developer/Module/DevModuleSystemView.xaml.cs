using CleanerControlApp.Hardwares;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;

namespace CleanerControlApp.Vision.Developer.Module
{
 public partial class DevModuleSystemView : UserControl
 {
 private readonly HardwareManager? _hw;
 private readonly DispatcherTimer _timer;

 public DevModuleSystemView()
 {
 InitializeComponent();

 try
 {
 if (App.AppHost != null)
 {
 _hw = App.AppHost.Services.GetService<HardwareManager>();
 }
 }
 catch { _hw = null; }

 _timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(500) };
 _timer.Tick += Timer_Tick;
 Loaded += (s, e) => _timer.Start();
 Unloaded += (s, e) => _timer.Stop();

 UpdateStatus();
 }

 private void Timer_Tick(object? sender, EventArgs e)
 {
 UpdateStatus();
 }

 private void UpdateStatus()
 {
 try
 {
 if (_hw != null)
 {
 txtDryRunStatus.Text = _hw.DryRunProcedureStatus.ToString();
 txtDryRunStatusString.Text = _hw.DryRunProcedureStatusString ?? string.Empty;

 // update PassClamperCheckCassette button appearance
 bool pass = _hw.ShuttlePassClamperCheckCassette;
 btnPassClamperCheckCassette.Background = pass ? Brushes.LightGreen : Brushes.LightGray;
 btnPassClamperCheckCassette.Content = pass ? "自動不檢查卡匣: ON" : "自動不檢查卡匣: OFF";
 }
 else
 {
 txtDryRunStatus.Text = "N/A";
 txtDryRunStatusString.Text = string.Empty;
 btnPassClamperCheckCassette.Background = Brushes.LightGray;
 btnPassClamperCheckCassette.Content = "自動不檢查卡匣";
 }
 }
 catch { }
 }

 private void BtnStartDryRun_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try
 {
 _hw?.StartDryRunProcedure();
 UpdateStatus();
 }
 catch { }
 }

 private void BtnStopDryRun_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try
 {
 _hw?.StopDryRunProcedure();
 UpdateStatus();
 }
 catch { }
 }

 // Click handler for Pass Clamper Check Cassette button
 private void btnPassClamperCheckCassette_Click(object sender, RoutedEventArgs e)
 {
 try
 {
 if (_hw != null)
 {
 _hw.ShuttlePassClamperCheckCassette = !_hw.ShuttlePassClamperCheckCassette;
 }
 UpdateStatus();
 }
 catch { }
 }
 }
}