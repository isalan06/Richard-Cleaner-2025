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
using CleanerControlApp.Hardwares.Shuttle.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_Clamper.xaml 的互動邏輯
    /// </summary>
    public partial class Template_Clamper : UserControl, INotifyPropertyChanged
    {
        private readonly IShuttle? _shuttle;
        private readonly DispatcherTimer _timer;

        private bool _sensorClamperFOpen;
        private bool _sensorClamperFClose;
        private bool _sensorClamperBOpen;
        private bool _sensorClamperBClose;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Template_Clamper()
        {
            InitializeComponent();

            try
            {
                _shuttle = App.AppHost?.Services.GetService<IShuttle>();
            }
            catch
            {
                _shuttle = null;
            }

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _timer.Tick += Timer_Tick;

            Loaded += (s, e) => _timer.Start();
            Unloaded += (s, e) => _timer.Stop();

            UpdateFromShuttle();
            DataContext = this;
        }

        public bool Sensor_ClamperFOpen
        {
            get => _sensorClamperFOpen;
            private set
            {
                if (_sensorClamperFOpen != value)
                {
                    _sensorClamperFOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Sensor_ClamperFClose
        {
            get => _sensorClamperFClose;
            private set
            {
                if (_sensorClamperFClose != value)
                {
                    _sensorClamperFClose = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Sensor_ClamperBOpen
        {
            get => _sensorClamperBOpen;
            private set
            {
                if (_sensorClamperBOpen != value)
                {
                    _sensorClamperBOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Sensor_ClamperBClose
        {
            get => _sensorClamperBClose;
            private set
            {
                if (_sensorClamperBClose != value)
                {
                    _sensorClamperBClose = value;
                    OnPropertyChanged();
                }
            }
        }

        // DependencyProperties for XAML ElementName bindings
        public static readonly DependencyProperty Sensor_Exist1Property = DependencyProperty.Register(
            "Sensor_Exist1", typeof(bool), typeof(Template_Clamper), new PropertyMetadata(false));

        public bool Sensor_Exist1
        {
            get => (bool)GetValue(Sensor_Exist1Property);
            private set => SetValue(Sensor_Exist1Property, value);
        }

        public static readonly DependencyProperty Sensor_Exist2Property = DependencyProperty.Register(
            "Sensor_Exist2", typeof(bool), typeof(Template_Clamper), new PropertyMetadata(false));

        public bool Sensor_Exist2
        {
            get => (bool)GetValue(Sensor_Exist2Property);
            private set => SetValue(Sensor_Exist2Property, value);
        }

        public static readonly DependencyProperty ClamperOpenProperty = DependencyProperty.Register(
            "ClamperOpen", typeof(bool), typeof(Template_Clamper), new PropertyMetadata(false));

        public bool ClamperOpen
        {
            get => (bool)GetValue(ClamperOpenProperty);
            private set => SetValue(ClamperOpenProperty, value);
        }

        public static readonly DependencyProperty ClamperCloseProperty = DependencyProperty.Register(
            "ClamperClose", typeof(bool), typeof(Template_Clamper), new PropertyMetadata(false));

        public bool ClamperClose
        {
            get => (bool)GetValue(ClamperCloseProperty);
            private set => SetValue(ClamperCloseProperty, value);
        }

        // NEW: DependencyProperties for Sensor_ClamperOpen and Sensor_ClamperClose
        public static readonly DependencyProperty Sensor_ClamperOpenProperty = DependencyProperty.Register(
            "Sensor_ClamperOpen", typeof(bool), typeof(Template_Clamper), new PropertyMetadata(false));

        public bool Sensor_ClamperOpen
        {
            get => (bool)GetValue(Sensor_ClamperOpenProperty);
            private set => SetValue(Sensor_ClamperOpenProperty, value);
        }

        public static readonly DependencyProperty Sensor_ClamperCloseProperty = DependencyProperty.Register(
            "Sensor_ClamperClose", typeof(bool), typeof(Template_Clamper), new PropertyMetadata(false));

        public bool Sensor_ClamperClose
        {
            get => (bool)GetValue(Sensor_ClamperCloseProperty);
            private set => SetValue(Sensor_ClamperCloseProperty, value);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateFromShuttle();
        }

        private void UpdateFromShuttle()
        {
            try
            {
                if (_shuttle != null)
                {
                    Sensor_ClamperFOpen = _shuttle.Sensor_ClamperFrontOpen;
                    Sensor_ClamperFClose = _shuttle.Sensor_ClamperFrontClose;
                    Sensor_ClamperBOpen = _shuttle.Sensor_ClamperBackOpen;
                    Sensor_ClamperBClose = _shuttle.Sensor_ClamperBackClose;

                    // Populate combined sensor properties used by XAML
                    Sensor_ClamperOpen = _shuttle.Sensor_ClamperOpen;
                    Sensor_ClamperClose = _shuttle.Sensor_ClamperClose;

                    Sensor_Exist1 = _shuttle.Sensor_CassetteExist1;
                    Sensor_Exist2 = _shuttle.Sensor_CassetteExist2;

                    ClamperOpen = _shuttle.Command_ClamperOpen;
                    ClamperClose = _shuttle.Command_ClamperClose;
                }
                else
                {
                    Sensor_ClamperFOpen = false;
                    Sensor_ClamperFClose = false;
                    Sensor_ClamperBOpen = false;
                    Sensor_ClamperBClose = false;

                    Sensor_ClamperOpen = false;
                    Sensor_ClamperClose = false;

                    Sensor_Exist1 = false;
                    Sensor_Exist2 = false;

                    ClamperOpen = false;
                    ClamperClose = false;
                }
            }
            catch
            {
                // ignore exceptions
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
