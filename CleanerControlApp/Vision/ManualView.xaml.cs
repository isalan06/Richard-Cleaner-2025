using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CleanerControlApp.Vision.Manual;

namespace CleanerControlApp.Vision
{
    /// <summary>
    /// ManualView.xaml 的互動邏輯
    /// </summary>
    public partial class ManualView : UserControl
    {
        private readonly Brush _selectedBg = new SolidColorBrush(Color.FromRgb(0x00,0x33,0x66));
        private readonly Brush _selectedFg = Brushes.White;
        private readonly Brush _unselectedBg = new SolidColorBrush(Color.FromRgb(0x87,0xCE,0xFA));
        private readonly Brush _unselectedFg = Brushes.Black;

        private ManualSystemView? _systemView;
        private ManualShuttleView? _shuttleView;
        private ManualSinkView? _sinkView;
        private ManualSoakingTankView? _soakingView;
        private ManualDryingTankView? _dryingView;
        private ManualHeatingTankView? _heatingView;

        private enum Tab { System, Shuttle, Sink, Soaking, Drying, Heating }

        public ManualView()
        {
            InitializeComponent();

            Loaded += ManualView_Loaded;
        }

        private void ManualView_Loaded(object? sender, RoutedEventArgs e)
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
                    if (_systemView == null) _systemView = new ManualSystemView();
                    ManualContentPlaceholder.Content = _systemView;
                    break;
                case Tab.Shuttle:
                    BtnShuttle.Background = _selectedBg; BtnShuttle.Foreground = _selectedFg;
                    if (_shuttleView == null) _shuttleView = new ManualShuttleView();
                    ManualContentPlaceholder.Content = _shuttleView;
                    break;
                case Tab.Sink:
                    BtnSink.Background = _selectedBg; BtnSink.Foreground = _selectedFg;
                    if (_sinkView == null) _sinkView = new ManualSinkView();
                    ManualContentPlaceholder.Content = _sinkView;
                    break;
                case Tab.Soaking:
                    BtnSoakingTank.Background = _selectedBg; BtnSoakingTank.Foreground = _selectedFg;
                    if (_soakingView == null) _soakingView = new ManualSoakingTankView();
                    ManualContentPlaceholder.Content = _soakingView;
                    break;
                case Tab.Drying:
                    BtnDryingTank.Background = _selectedBg; BtnDryingTank.Foreground = _selectedFg;
                    if (_dryingView == null) _dryingView = new ManualDryingTankView();
                    ManualContentPlaceholder.Content = _dryingView;
                    break;
                case Tab.Heating:
                    BtnHeatingTank.Background = _selectedBg; BtnHeatingTank.Foreground = _selectedFg;
                    if (_heatingView == null) _heatingView = new ManualHeatingTankView();
                    ManualContentPlaceholder.Content = _heatingView;
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
