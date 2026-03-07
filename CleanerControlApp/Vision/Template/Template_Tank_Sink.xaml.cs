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
using CleanerControlApp.Hardwares.Sink.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_Tank_Sink.xaml 的互動邏輯
    /// </summary>
    public partial class Template_Tank_Sink : UserControl, INotifyPropertyChanged
    {
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private double _origLeft;
        private double _origTop;

        private readonly ISink? _sink;
        private readonly DispatcherTimer _timer;

        private bool _autoStatus;
        private bool _pauseStatus;
        private bool _cassetteStatus;
        private bool _idleStatus;
        private bool _initializedStatus;
        private bool _warningStatus;
        private bool _alarmStatus;

        // new properties mapped to ISink
        private bool _commandCoverClose;
        private bool _sensorCoverOpen;
        private bool _sensorCoverClose;
        private bool _commandAirOpen;

        // HS_ properties
        private bool _pickFinished;
        private bool _placeFinished;
        private bool _moving;
        private bool _inputPermit;
        private bool _actFinished;

        // elapsed / remaining display
        private string _elapsedAct = "00:00";
        private string _remainingAct = "00:00";

        public event PropertyChangedEventHandler? PropertyChanged;

        public Template_Tank_Sink()
        {
            InitializeComponent();

            try
            {
                _sink = App.AppHost?.Services.GetService<ISink>();
            }
            catch
            {
                _sink = null;
            }

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _timer.Tick += Timer_Tick;

            Loaded += (s, e) => _timer.Start();
            Unloaded += (s, e) => _timer.Stop();

            UpdateFromSink();
        }

        public bool AutoStatus
        {
            get => _autoStatus;
            private set
            {
                if (_autoStatus != value)
                {
                    _autoStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool PauseStatus
        {
            get => _pauseStatus;
            private set
            {
                if (_pauseStatus != value)
                {
                    _pauseStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CassetteStatus
        {
            get => _cassetteStatus;
            private set
            {
                if (_cassetteStatus != value)
                {
                    _cassetteStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IdleStatus
        {
            get => _idleStatus;
            private set
            {
                if (_idleStatus != value)
                {
                    _idleStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool InitializedStatus
        {
            get => _initializedStatus;
            private set
            {
                if (_initializedStatus != value)
                {
                    _initializedStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool WarningStatus
        {
            get => _warningStatus;
            private set
            {
                if (_warningStatus != value)
                {
                    _warningStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AlarmStatus
        {
            get => _alarmStatus;
            private set
            {
                if (_alarmStatus != value)
                {
                    _alarmStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        // CommandCoverClose <- ISink.Command_CleanerCoverClose
        public bool CommandCoverClose
        {
            get => _commandCoverClose;
            private set
            {
                if (_commandCoverClose != value)
                {
                    _commandCoverClose = value;
                    OnPropertyChanged();
                }
            }
        }

        // SensorCoverOpen <- ISink.Sensor_CoverOpen
        public bool SensorCoverOpen
        {
            get => _sensorCoverOpen;
            private set
            {
                if (_sensorCoverOpen != value)
                {
                    _sensorCoverOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        // SensorCoverClose <- ISink.Sensor_CoverClose
        public bool SensorCoverClose
        {
            get => _sensorCoverClose;
            private set
            {
                if (_sensorCoverClose != value)
                {
                    _sensorCoverClose = value;
                    OnPropertyChanged();
                }
            }
        }

        // CommandAirOpen <- ISink.Command_CleanerAirOpen
        public bool CommandAirOpen
        {
            get => _commandAirOpen;
            private set
            {
                if (_commandAirOpen != value)
                {
                    _commandAirOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        // HS_ClamperPickFinished -> PickFinished
        public bool PickFinished
        {
            get => _pickFinished;
            private set
            {
                if (_pickFinished != value)
                {
                    _pickFinished = value;
                    OnPropertyChanged();
                }
            }
        }

        // HS_ClamperPlaceFinished -> PlaceFinished
        public bool PlaceFinished
        {
            get => _placeFinished;
            private set
            {
                if (_placeFinished != value)
                {
                    _placeFinished = value;
                    OnPropertyChanged();
                }
            }
        }

        // HS_ClamperMoving -> Moving
        public bool Moving
        {
            get => _moving;
            private set
            {
                if (_moving != value)
                {
                    _moving = value;
                    OnPropertyChanged();
                }
            }
        }

        // HS_InputPermit -> InputPermit
        public bool InputPermit
        {
            get => _inputPermit;
            private set
            {
                if (_inputPermit != value)
                {
                    _inputPermit = value;
                    OnPropertyChanged();
                }
            }
        }

        // HS_ActFinished -> ActFinished
        public bool ActFinished
        {
            get => _actFinished;
            private set
            {
                if (_actFinished != value)
                {
                    _actFinished = value;
                    OnPropertyChanged();
                }
            }
        }

        // Elapsed and Remaining display properties (formatted mm:ss)
        public string ElapsedAct
        {
            get => _elapsedAct;
            private set
            {
                if (_elapsedAct != value)
                {
                    _elapsedAct = value;
                    OnPropertyChanged();
                }
            }
        }

        public string RemainingAct
        {
            get => _remainingAct;
            private set
            {
                if (_remainingAct != value)
                {
                    _remainingAct = value;
                    OnPropertyChanged();
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateFromSink();
        }

        private void UpdateFromSink()
        {
            try
            {
                if (_sink != null)
                {
                    AutoStatus = _sink.Auto;
                    PauseStatus = _sink.Pausing;
                    CassetteStatus = _sink.Cassette;
                    IdleStatus = _sink.Idle;
                    InitializedStatus = _sink.Initialized;
                    WarningStatus = _sink.HasWarning;
                    AlarmStatus = _sink.HasAlarm;

                    // new mappings
                    CommandCoverClose = _sink.Command_CleanerCoverClose;
                    SensorCoverOpen = _sink.Sensor_CoverOpen;
                    SensorCoverClose = _sink.Sensor_CoverClose;
                    CommandAirOpen = _sink.Command_CleanerAirOpen;

                    // HS_ mappings
                    PickFinished = _sink.HS_ClamperPickFinished;
                    PlaceFinished = _sink.HS_ClamperPlaceFinished;
                    Moving = _sink.HS_ClamperMoving;
                    InputPermit = _sink.HS_InputPermit;
                    ActFinished = _sink.HS_ActFinished;

                    // elapsed/remaining
                    try
                    {
                        ElapsedAct = FormatTime(_sink.ElpasedPressureTime_Seconds);
                        RemainingAct = FormatTime(_sink.RemainingPressureTime_Seconds);
                    }
                    catch
                    {
                        ElapsedAct = "00:00";
                        RemainingAct = "00:00";
                    }
                }
                else
                {
                    AutoStatus = false;
                    PauseStatus = false;
                    CassetteStatus = false;
                    IdleStatus = false;
                    InitializedStatus = false;
                    WarningStatus = false;
                    AlarmStatus = false;

                    CommandCoverClose = false;
                    SensorCoverOpen = false;
                    SensorCoverClose = false;
                    CommandAirOpen = false;

                    PickFinished = false;
                    PlaceFinished = false;
                    Moving = false;
                    InputPermit = false;
                    ActFinished = false;

                    ElapsedAct = "00:00";
                    RemainingAct = "00:00";
                }
            }
            catch
            {
                // ignore
            }
        }

        private static string FormatTime(int seconds)
        {
            if (seconds <0) seconds =0;
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.ToString(@"mm\:ss");
        }

        private void Group_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement el)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(MainCanvas);
                _origLeft = Canvas.GetLeft(tankGroup);
                _origTop = Canvas.GetTop(tankGroup);
                el.CaptureMouse();
            }
        }

        private void Group_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point pos = e.GetPosition(MainCanvas);
                double dx = pos.X - _dragStartPoint.X;
                double dy = pos.Y - _dragStartPoint.Y;
                Canvas.SetLeft(tankGroup, _origLeft + dx);
                Canvas.SetTop(tankGroup, _origTop + dy);
            }
        }

        private void Group_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                if (sender is UIElement el)
                    el.ReleaseMouseCapture();
            }
        }
    }
}
