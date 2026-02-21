using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using CleanerControlApp.Modules.MitsubishiPLC.Models;

namespace CleanerControlApp.Vision.Developer
{
 /// <summary>
 /// Interaction logic for PlcTestView.xaml
 /// </summary>
 public partial class PlcTestView : UserControl
 {
 private readonly DispatcherTimer _refreshTimer;

 private enum Tab
 {
 DI,
 DO,
 Motor,
 Param
 }

 // tab views (lazy)
 private UserControl? _diView;
 private UserControl? _doView;
 private UserControl? _motorView;
 private UserControl? _paramView;

 // brushes for selected/unselected
 private readonly Brush _selectedBg = new SolidColorBrush(Color.FromRgb(0x00,0x33,0x66)); // 深藍
 private readonly Brush _selectedFg = Brushes.White;
 private readonly Brush _unselectedBg = new SolidColorBrush(Color.FromRgb(0x87,0xCE,0xFA)); // 天藍
 private readonly Brush _unselectedFg = Brushes.Black;

 public PlcTestView()
 {
 InitializeComponent();

 // At runtime set DataContext to IPLCService provided by host if available
 if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
 {
 // provide a simple design-time DataContext
 this.DataContext = new DesignTimePlcViewModel();
 }
 else
 {
 var svc = App.AppHost?.Services.GetService(typeof(IPLCService)) as IPLCService;
 if (svc != null)
 {
 this.DataContext = svc;
 }
 }

 // create and start timer to update running status every second
 _refreshTimer = new DispatcherTimer
 {
 Interval = TimeSpan.FromSeconds(1)
 };
 _refreshTimer.Tick += RefreshTimer_Tick;
 Loaded += PlcTestView_Loaded;
 Unloaded += PlcTestView_Unloaded;
 }

 private void PlcTestView_Loaded(object? sender, System.Windows.RoutedEventArgs e)
 {
 _refreshTimer.Start();
 // refresh immediately
 RefreshRunningIndicator();

 // initialize tab button styles and show default DI page
 InitializeTabButtons();
 SelectTab(Tab.DI);
 }

 private void PlcTestView_Unloaded(object? sender, System.Windows.RoutedEventArgs e)
 {
 _refreshTimer.Stop();
 _refreshTimer.Tick -= RefreshTimer_Tick;
 }

 private void RefreshTimer_Tick(object? sender, EventArgs e)
 {
 RefreshRunningIndicator();
 }

 private void RefreshRunningIndicator()
 {
 try
 {
 var svc = this.DataContext as IPLCService ?? App.AppHost?.Services.GetService(typeof(IPLCService)) as IPLCService;
 bool isRunning = svc?.IsRunning ?? false;

 RunningIndicator.Fill = isRunning ? Brushes.Green : Brushes.Red;
 // Map IsRunning=true -> 運轉, false -> 停止
 RunningText.Text = isRunning ? "運轉" : "停止";
 }
 catch
 {
 // ignore any exceptions during refresh
 }
 }

 private void BtnRun_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 var svc = this.DataContext as IPLCService ?? App.AppHost?.Services.GetService(typeof(IPLCService)) as IPLCService;
 svc?.Start();
 RefreshRunningIndicator();
 }

 private void BtnStop_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 var svc = this.DataContext as IPLCService ?? App.AppHost?.Services.GetService(typeof(IPLCService)) as IPLCService;
 svc?.Stop();
 RefreshRunningIndicator();
 }

 // Tab button click handlers
 private void BtnTabDI_Click(object sender, System.Windows.RoutedEventArgs e) => SelectTab(Tab.DI);
 private void BtnTabDO_Click(object sender, System.Windows.RoutedEventArgs e) => SelectTab(Tab.DO);
 private void BtnTabMotor_Click(object sender, System.Windows.RoutedEventArgs e) => SelectTab(Tab.Motor);
 private void BtnTabParam_Click(object sender, System.Windows.RoutedEventArgs e) => SelectTab(Tab.Param);

 private void InitializeTabButtons()
 {
 // set default unselected styles
 BtnTabDI.Background = _unselectedBg; BtnTabDI.Foreground = _unselectedFg;
 BtnTabDO.Background = _unselectedBg; BtnTabDO.Foreground = _unselectedFg;
 BtnTabMotor.Background = _unselectedBg; BtnTabMotor.Foreground = _unselectedFg;
 BtnTabParam.Background = _unselectedBg; BtnTabParam.Foreground = _unselectedFg;

 // remove default focus styling that may confuse colors
 BtnTabDI.Focusable = false;
 BtnTabDO.Focusable = false;
 BtnTabMotor.Focusable = false;
 BtnTabParam.Focusable = false;
 }

 private void SelectTab(Tab tab)
 {
 // reset all to unselected
 BtnTabDI.Background = _unselectedBg; BtnTabDI.Foreground = _unselectedFg;
 BtnTabDO.Background = _unselectedBg; BtnTabDO.Foreground = _unselectedFg;
 BtnTabMotor.Background = _unselectedBg; BtnTabMotor.Foreground = _unselectedFg;
 BtnTabParam.Background = _unselectedBg; BtnTabParam.Foreground = _unselectedFg;

 // set selected button style and load content
 switch (tab)
 {
 case Tab.DI:
 BtnTabDI.Background = _selectedBg; BtnTabDI.Foreground = _selectedFg;
 if (_diView == null) _diView = new PlcTest_DIView();
 TabContentPlaceholder.Content = _diView;
 break;
 case Tab.DO:
 BtnTabDO.Background = _selectedBg; BtnTabDO.Foreground = _selectedFg;
 if (_doView == null) _doView = new PlcTest_DOView();
 TabContentPlaceholder.Content = _doView;
 break;
 case Tab.Motor:
 BtnTabMotor.Background = _selectedBg; BtnTabMotor.Foreground = _selectedFg;
 if (_motorView == null) _motorView = new PlcTest_MotorStatusView();
 TabContentPlaceholder.Content = _motorView;
 break;
 case Tab.Param:
 BtnTabParam.Background = _selectedBg; BtnTabParam.Foreground = _selectedFg;
 if (_paramView == null) _paramView = new PlcTest_ParameterView();
 TabContentPlaceholder.Content = _paramView;
 break;
 }
 }

 // Simple design-time implementation of IPLCService to allow XAML designer to show values
 private class DesignTimePlcViewModel : IPLCService
 {
 public PLC_Bit_Union[] DIO_X { get; set; } = new PLC_Bit_Union[5];
 public PLC_Bit_Union[] DIO_Y { get; set; } = new PLC_Bit_Union[4];
 public PLC_Bit_Union[] StatusIO { get; set; } = new PLC_Bit_Union[7];
 public PLC_DWord_Union[] MotionPos { get; set; } = new PLC_DWord_Union[4];
 public PLC_Bit_Union[] Command { get; set; } = new PLC_Bit_Union[9];
 public PLC_DWord_Union[] MoveInfo { get; set; } = new PLC_DWord_Union[8];
 public PLC_DWord_Union[] ParamMotionInfo { get; set; } = new PLC_DWord_Union[24];
 public PLC_Word_Union[] ParamTimeout { get; set; } = new PLC_Word_Union[8];
 public PLC_DWord_Union[] ParamMotionInfoW { get; set; } = new PLC_DWord_Union[24];
 public PLC_Word_Union[] ParamTimeoutW { get; set; } = new PLC_Word_Union[8];

 public bool IsRunning => true;

 public void Start() { }
 public void Stop() { }
 public void ReadParameter() { }
 public void WriteParameter() { }
 }
 }
}