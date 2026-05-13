using CleanerControlApp.Hardwares;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Controls;
using System.Windows.Threading;

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
 }
 else
 {
 txtDryRunStatus.Text = "N/A";
 txtDryRunStatusString.Text = string.Empty;
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
 }
}