using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using CleanerControlApp.Hardwares;

namespace CleanerControlApp.Vision.SystemTemplate
{
    /// <summary>
    /// Template_Loader.xaml 的互動邏輯
    /// </summary>
    public partial class Template_Loader : UserControl, INotifyPropertyChanged
    {
        private readonly DispatcherTimer _timer;
        private readonly HardwareManager? _hardwareManager;

        private int _cassetteCount;
        private bool _sensor1;
        private bool _sensor2;
        private bool _sensor3;
        private bool _sensor4;
        private bool _sensor5;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Template_Loader()
        {
            InitializeComponent();

            try
            {
                _hardwareManager = App.AppHost?.Services.GetService<HardwareManager>();
            }
            catch
            {
                _hardwareManager = null;
            }

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _timer.Tick += Timer_Tick;

            Loaded += Template_Loader_Loaded;
            Unloaded += Template_Loader_Unloaded;
        }

        private void Template_Loader_Loaded(object? sender, RoutedEventArgs e)
        {
            DataContext = this; // expose properties for XAML bindings
            _timer.Start();
            // initial update
            UpdateFromHardware();
        }

        private void Template_Loader_Unloaded(object? sender, RoutedEventArgs e)
        {
            _timer.Stop();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateFromHardware();
        }

        private void UpdateFromHardware()
        {
            try
            {
                if (_hardwareManager != null)
                {
                    LoaderCassetteCount = _hardwareManager.LoaderCassetteCount;
                    LoaderCassetteInPosition1 = _hardwareManager.LoaderCassetteInPosition1;
                    LoaderCassetteInPosition2 = _hardwareManager.LoaderCassetteInPosition2;
                    LoaderCassetteInPosition3 = _hardwareManager.LoaderCassetteInPosition3;
                    LoaderCassetteInPosition4 = _hardwareManager.LoaderCassetteInPosition4;
                    LoaderCassetteInPosition5 = _hardwareManager.LoaderCassetteInPosition5;
                }
                else
                {
                    LoaderCassetteCount =0;
                    LoaderCassetteInPosition1 = false;
                    LoaderCassetteInPosition2 = false;
                    LoaderCassetteInPosition3 = false;
                    LoaderCassetteInPosition4 = false;
                    LoaderCassetteInPosition5 = false;
                }
            }
            catch
            {
                // ignore update exceptions
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public int LoaderCassetteCount
        {
            get => _cassetteCount;
            private set
            {
                if (_cassetteCount != value)
                {
                    _cassetteCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool LoaderCassetteInPosition1
        {
            get => _sensor1;
            private set
            {
                if (_sensor1 != value)
                {
                    _sensor1 = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool LoaderCassetteInPosition2
        {
            get => _sensor2;
            private set
            {
                if (_sensor2 != value)
                {
                    _sensor2 = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool LoaderCassetteInPosition3
        {
            get => _sensor3;
            private set
            {
                if (_sensor3 != value)
                {
                    _sensor3 = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool LoaderCassetteInPosition4
        {
            get => _sensor4;
            private set
            {
                if (_sensor4 != value)
                {
                    _sensor4 = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool LoaderCassetteInPosition5
        {
            get => _sensor5;
            private set
            {
                if (_sensor5 != value)
                {
                    _sensor5 = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
