using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CleanerControlApp.Vision.SettingViews;

namespace CleanerControlApp.Vision
{
    /// <summary>
    /// SettingView.xaml 的互動邏輯
    /// </summary>
    public partial class SettingView : UserControl
    {
        // brushes for selected/unselected - match IOView colors
        private readonly Brush _selectedBg = new SolidColorBrush(Color.FromRgb(0x00,0x33,0x66));
        private readonly Brush _selectedFg = Brushes.White;
        private readonly Brush _unselectedBg = new SolidColorBrush(Color.FromRgb(0x87,0xCE,0xFA));
        private readonly Brush _unselectedFg = Brushes.Black;

        // cached view instances
        private SetSystemView? _systemView;
        private SetShuttleView? _shuttleView;
        private SetSinkView? _sinkView;
        private SetSoakingTankView? _soakingView;
        private SetDryinTankView? _dryingView;
        private SetHeatingTankView? _heatingView;

        private enum Tab { System, Shuttle, Sink, Soaking, Drying, Heating }

        public SettingView()
        {
            InitializeComponent();

            Loaded += SettingView_Loaded;
        }

        private void SettingView_Loaded(object? sender, RoutedEventArgs e)
        {
            InitializeTabButtons();
            SelectTab(Tab.System);
        }

        private void InitializeTabButtons()
        {
            // set default unselected styles
            BtnSystem.Background = _unselectedBg; BtnSystem.Foreground = _unselectedFg;
            BtnShuttle.Background = _unselectedBg; BtnShuttle.Foreground = _unselectedFg;
            BtnSink.Background = _unselectedBg; BtnSink.Foreground = _unselectedFg;
            BtnSoakingTank.Background = _unselectedBg; BtnSoakingTank.Foreground = _unselectedFg;
            BtnDryingTank.Background = _unselectedBg; BtnDryingTank.Foreground = _unselectedFg;
            BtnHeatingTank.Background = _unselectedBg; BtnHeatingTank.Foreground = _unselectedFg;

            // remove focusable to avoid focus rectangle affecting colors
            BtnSystem.Focusable = false;
            BtnShuttle.Focusable = false;
            BtnSink.Focusable = false;
            BtnSoakingTank.Focusable = false;
            BtnDryingTank.Focusable = false;
            BtnHeatingTank.Focusable = false;
        }

        private void SelectTab(Tab tab)
        {
            // reset all to unselected
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
                    if (_systemView == null) _systemView = new SetSystemView();
                    TabContentPlaceholder.Content = _systemView;
                    break;
                case Tab.Shuttle:
                    BtnShuttle.Background = _selectedBg; BtnShuttle.Foreground = _selectedFg;
                    if (_shuttleView == null) _shuttleView = new SetShuttleView();
                    TabContentPlaceholder.Content = _shuttleView;
                    break;
                case Tab.Sink:
                    BtnSink.Background = _selectedBg; BtnSink.Foreground = _selectedFg;
                    if (_sinkView == null) _sinkView = new SetSinkView();
                    TabContentPlaceholder.Content = _sinkView;
                    break;
                case Tab.Soaking:
                    BtnSoakingTank.Background = _selectedBg; BtnSoakingTank.Foreground = _selectedFg;
                    if (_soakingView == null) _soakingView = new SetSoakingTankView();
                    TabContentPlaceholder.Content = _soakingView;
                    break;
                case Tab.Drying:
                    BtnDryingTank.Background = _selectedBg; BtnDryingTank.Foreground = _selectedFg;
                    if (_dryingView == null) _dryingView = new SetDryinTankView();
                    TabContentPlaceholder.Content = _dryingView;
                    break;
                case Tab.Heating:
                    BtnHeatingTank.Background = _selectedBg; BtnHeatingTank.Foreground = _selectedFg;
                    if (_heatingView == null) _heatingView = new SetHeatingTankView();
                    TabContentPlaceholder.Content = _heatingView;
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
