using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CleanerControlApp.Vision
{
    /// <summary>
    /// IOView.xaml 的互動邏輯
    /// </summary>
    public partial class IOView : UserControl
    {
        private IO.IO_DIView _diView;
        private IO.IO_DOView _doView;

        private enum Tab { DI, DO }

        // brushes for selected/unselected
        private readonly Brush _selectedBg = new SolidColorBrush(Color.FromRgb(0x00,0x33,0x66));
        private readonly Brush _selectedFg = Brushes.White;
        private readonly Brush _unselectedBg = new SolidColorBrush(Color.FromRgb(0x87,0xCE,0xFA));
        private readonly Brush _unselectedFg = Brushes.Black;

        public IOView()
        {
            InitializeComponent();

            // instantiate views
            _diView = new IO.IO_DIView();
            _doView = new IO.IO_DOView();

            // default content will be set after Loaded so styles are initialized
            Loaded += IOView_Loaded;
        }

        private void IOView_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeTabButtons();
            SelectTab(Tab.DI);
        }

        private void InitializeTabButtons()
        {
            // set default unselected styles
            BtnTabDI.Background = _unselectedBg; BtnTabDI.Foreground = _unselectedFg;
            BtnTabDO.Background = _unselectedBg; BtnTabDO.Foreground = _unselectedFg;

            // remove focusable to avoid focus rectangle affecting colors
            BtnTabDI.Focusable = false;
            BtnTabDO.Focusable = false;
        }

        private void SelectTab(Tab tab)
        {
            // reset all to unselected
            BtnTabDI.Background = _unselectedBg; BtnTabDI.Foreground = _unselectedFg;
            BtnTabDO.Background = _unselectedBg; BtnTabDO.Foreground = _unselectedFg;

            switch (tab)
            {
                case Tab.DI:
                    BtnTabDI.Background = _selectedBg; BtnTabDI.Foreground = _selectedFg;
                    if (_diView == null) _diView = new IO.IO_DIView();
                    TabContentPlaceholder.Content = _diView;
                    break;
                case Tab.DO:
                    BtnTabDO.Background = _selectedBg; BtnTabDO.Foreground = _selectedFg;
                    if (_doView == null) _doView = new IO.IO_DOView();
                    TabContentPlaceholder.Content = _doView;
                    break;
            }
        }

        private void BtnTabDI_Click(object sender, RoutedEventArgs e)
        {
            SelectTab(Tab.DI);
        }

        private void BtnTabDO_Click(object sender, RoutedEventArgs e)
        {
            SelectTab(Tab.DO);
        }
    }
}
