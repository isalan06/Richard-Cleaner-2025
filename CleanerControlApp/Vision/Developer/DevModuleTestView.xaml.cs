using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace CleanerControlApp.Vision.Developer
{
 public partial class DevModuleTestView : UserControl
 {
 private readonly Brush _selectedBg = new SolidColorBrush(Color.FromRgb(0x00,0x33,0x66));
 private readonly Brush _selectedFg = Brushes.White;
 private readonly Brush _unselectedBg = new SolidColorBrush(Color.FromRgb(0x87,0xCE,0xFA));
 private readonly Brush _unselectedFg = Brushes.Black;

 private Module.DevModuleSystemView? _systemView;
 private Module.DevModuleShuttleView? _shuttleView;
 private Module.DevModuleSinkView? _sinkView;
 private Module.DevModuleSoakingTankView? _soakingView;
 private Module.DevModuleDryingTankView? _dryingView;
 private Module.DevModuleHeatingTankView? _heatingView;

 private enum Tab { System, Shuttle, Sink, Soaking, Drying, Heating }

 public DevModuleTestView()
 {
 InitializeComponent();
 Loaded += DevModuleTestView_Loaded;
 }

 private void DevModuleTestView_Loaded(object? sender, RoutedEventArgs e)
 {
 InitializeTabButtons();
 SelectTab(Tab.System);
 }

 private void InitializeTabButtons()
 {
 BtnSystem.Background = _unselectedBg; BtnSystem.Foreground = _unselectedFg;
 BtnShuttle.Background = _unselectedBg; BtnShuttle.Foreground = _unselectedFg;
 BtnSink.Background = _unselectedBg; BtnSink.Foreground = _unselectedFg;
 BtnSoakingTank.Background = _unselectedBg; BtnSoakingTank.Foreground = _unselectedFg;
 BtnDryingTank.Background = _unselectedBg; BtnDryingTank.Foreground = _unselectedFg;
 BtnHeatingTank.Background = _unselectedBg; BtnHeatingTank.Foreground = _unselectedFg;

 BtnSystem.Focusable = false;
 BtnShuttle.Focusable = false;
 BtnSink.Focusable = false;
 BtnSoakingTank.Focusable = false;
 BtnDryingTank.Focusable = false;
 BtnHeatingTank.Focusable = false;
 }

 private void SelectTab(Tab tab)
 {
 // reset
 BtnSystem.Background = _unselectedBg; BtnSystem.Foreground = _unselectedFg;
 BtnShuttle.Background = _unselectedBg; BtnShuttle.Foreground = _unselectedFg;
 BtnSink.Background = _unselectedBg; BtnSink.Foreground = _unselectedFg;
 BtnSoakingTank.Background = _unselectedBg; BtnSoakingTank.Foreground = _unselectedFg;
 BtnDryingTank.Background = _unselectedBg; BtnDryingTank.Foreground = _unselectedFg;
 BtnHeatingTank.Background = _unselectedBg; BtnHeatingTank.Foreground = _unselectedFg;

 switch (tab)
 {
 case Tab.System:
 BtnSystem.Background = _selectedBg; BtnSystem.Foreground = _selectedFg;
 if (_systemView == null) _systemView = new Module.DevModuleSystemView();
 ModuleContentPlaceholder.Content = _systemView;
 break;
 case Tab.Shuttle:
 BtnShuttle.Background = _selectedBg; BtnShuttle.Foreground = _selectedFg;
 if (_shuttleView == null) _shuttleView = new Module.DevModuleShuttleView();
 ModuleContentPlaceholder.Content = _shuttleView;
 break;
 case Tab.Sink:
 BtnSink.Background = _selectedBg; BtnSink.Foreground = _selectedFg;
 if (_sinkView == null) _sinkView = new Module.DevModuleSinkView();
 ModuleContentPlaceholder.Content = _sinkView;
 break;
 case Tab.Soaking:
 BtnSoakingTank.Background = _selectedBg; BtnSoakingTank.Foreground = _selectedFg;
 if (_soakingView == null) _soakingView = new Module.DevModuleSoakingTankView();
 ModuleContentPlaceholder.Content = _soakingView;
 break;
 case Tab.Drying:
 BtnDryingTank.Background = _selectedBg; BtnDryingTank.Foreground = _selectedFg;
 if (_dryingView == null) _dryingView = new Module.DevModuleDryingTankView();
 ModuleContentPlaceholder.Content = _dryingView;
 break;
 case Tab.Heating:
 BtnHeatingTank.Background = _selectedBg; BtnHeatingTank.Foreground = _selectedFg;
 if (_heatingView == null) _heatingView = new Module.DevModuleHeatingTankView();
 ModuleContentPlaceholder.Content = _heatingView;
 break;
 }
 }

 private void BtnSystem_Click(object sender, RoutedEventArgs e)
 {
 SelectTab(Tab.System);
 }

 private void BtnShuttle_Click(object sender, RoutedEventArgs e)
 {
 SelectTab(Tab.Shuttle);
 }

 private void BtnSink_Click(object sender, RoutedEventArgs e)
 {
 SelectTab(Tab.Sink);
 }

 private void BtnSoakingTank_Click(object sender, RoutedEventArgs e)
 {
 SelectTab(Tab.Soaking);
 }

 private void BtnDryingTank_Click(object sender, RoutedEventArgs e)
 {
 SelectTab(Tab.Drying);
 }

 private void BtnHeatingTank_Click(object sender, RoutedEventArgs e)
 {
 SelectTab(Tab.Heating);
 }
 }
}