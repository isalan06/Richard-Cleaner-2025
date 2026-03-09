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
using CleanerControlApp.Hardwares.SoakingTank.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_Tank_Soaking.xaml 的互動邏輯
    /// </summary>
    public partial class Template_Tank_Soaking : UserControl, INotifyPropertyChanged
    {
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private double _origLeft;
        private double _origTop;

        private readonly ISoakingTank? _soakingTank;
        private readonly DispatcherTimer _timer;

        private bool _autoStatus;
        private bool _pauseStatus;
        private bool _cassetteStatus;
        private bool _idleStatus;
        private bool _initializedStatus;
        private bool _warningStatus;
        private bool _alarmStatus;

        // new properties mapped to ISoakingTank
        private bool _commandCoverClose;
        private bool _sensorCoverOpen;
        private bool _sensorCoverClose;
        private bool _commandAirOpen;
        private bool _commandUltrasonicOpen;

        // HS_ properties
        private bool _pickFinished;
        private bool _placeFinished;
        private bool _moving;
        private bool _inputPermit;
        private bool _actFinished;
        private bool _requestWater;
        private bool _tankH;
        private bool _tankL;
        private bool _commandWaterOut;

        // elapsed / remaining display
        private string _elapsedAct = "00:00";
        private string _remainingAct = "00:00";

        public event PropertyChangedEventHandler? PropertyChanged;

        public Template_Tank_Soaking()
        {
            InitializeComponent();

            try
            {
                _soakingTank = App.AppHost?.Services.GetService<ISoakingTank>();
            }
            catch
            {
                _soakingTank = null;
            }

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _timer.Tick += Timer_Tick;

            Loaded += (s, e) => _timer.Start();
            Unloaded += (s, e) => _timer.Stop();

            UpdateFromSoakingTank();
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

        // CommandCoverClose <- ISoakingTank.Command_CleanerCoverClose
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

        // SensorCoverOpen <- ISoakingTank.Sensor_CoverOpen
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

        // SensorCoverClose <- ISoakingTank.Sensor_CoverClose
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

        // CommandAirOpen <- ISoakingTank.Command_CleanerAirOpen
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

        // CommandUltrasonicOpen <- ISoakingTank.Command_CleanerUltrasonicOpen
        public bool CommandUltrasonicOpen
        {
            get => _commandUltrasonicOpen;
            private set
            {
                if (_commandUltrasonicOpen != value)
                {
                    _commandUltrasonicOpen = value;
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

        // HS_RequestWater -> RequestWater
        public bool RequestWater
        {
            get => _requestWater;
            private set
            {
                if (_requestWater != value)
                {
                    _requestWater = value;
                    OnPropertyChanged();
                }
            }
        }

        // Command_CleanerWaterOutputOpen -> CommandWaterOut (for UI binding)
        public bool CommandWaterOut
        {
            get => _commandWaterOut;
            private set
            {
                if (_commandWaterOut != value)
                {
                    _commandWaterOut = value;
                    OnPropertyChanged();
                }
            }
        }

        // Tank liquid sensors
        public bool TankH
        {
            get => _tankH;
            private set
            {
                if (_tankH != value)
                {
                    _tankH = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool TankL
        {
            get => _tankL;
            private set
            {
                if (_tankL != value)
                {
                    _tankL = value;
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
            UpdateFromSoakingTank();
        }

        private void UpdateFromSoakingTank()
        {
            try
            {
                if (_soakingTank != null)
                {
                    AutoStatus = _soakingTank.Auto;
                    PauseStatus = _soakingTank.Pausing;
                    CassetteStatus = _soakingTank.Cassette;
                    IdleStatus = _soakingTank.Idle;
                    InitializedStatus = _soakingTank.Initialized;
                    WarningStatus = _soakingTank.HasWarning;
                    AlarmStatus = _soakingTank.HasAlarm;

                    // new mappings
                    CommandCoverClose = _soakingTank.Command_CleanerCoverClose;
                    SensorCoverOpen = _soakingTank.Sensor_CoverOpen;
                    SensorCoverClose = _soakingTank.Sensor_CoverClose;
                    CommandAirOpen = _soakingTank.Command_CleanerAirOpen;
                    CommandUltrasonicOpen = _soakingTank.Command_CleanerUltrasonicOpen;
                    RequestWater = _soakingTank.HS_RequestWater;
                    CommandWaterOut = _soakingTank.Command_CleanerWaterOutputOpen;
                    TankH = _soakingTank.Sensor_Liquid_H;
                    TankL = _soakingTank.Sensor_Liquid_L;

                    // HS_ mappings
                    PickFinished = _soakingTank.HS_ClamperPickFinished;
                    PlaceFinished = _soakingTank.HS_ClamperPlaceFinished;
                    Moving = _soakingTank.HS_ClamperMoving;
                    InputPermit = _soakingTank.HS_InputPermit;
                    ActFinished = _soakingTank.HS_ActFinished;

                    // elapsed/remaining
                    try
                    {
                        ElapsedAct = FormatTime(_soakingTank.ElpasedPressureTime_Seconds);
                        RemainingAct = FormatTime(_soakingTank.RemainingPressureTime_Seconds);
                    }
                    catch
                    {
                        ElapsedAct = "00:00";
                        RemainingAct = "00:00";
                    }

                    // Ultrasonic display values
                    try
                    {
                        if (txtSetCurrent != null)
                            txtSetCurrent.Text = _soakingTank.UD_SetCurrent.ToString("0.00");
                        if (txtActFreq != null)
                            txtActFreq.Text = _soakingTank.UD_Frequency.ToString("0.0");
                        if (txtTime != null)
                            txtTime.Text = _soakingTank.UD_Time.ToString();
                        if (txtPower != null)
                            txtPower.Text = _soakingTank.UD_Power.ToString();
                    }
                    catch
                    {
                        if (txtSetCurrent != null) txtSetCurrent.Text = "0.00";
                        if (txtActFreq != null) txtActFreq.Text = "0.0";
                        if (txtTime != null) txtTime.Text = "0";
                        if (txtPower != null) txtPower.Text = "0";
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
                    RequestWater = false;
                    TankH = false;
                    TankL = false;
                    CommandUltrasonicOpen = false;
                    CommandWaterOut = false;

                    PickFinished = false;
                    PlaceFinished = false;
                    Moving = false;
                    InputPermit = false;
                    ActFinished = false;

                    ElapsedAct = "00:00";
                    RemainingAct = "00:00";

                    // reset ultrasonic display values
                    if (txtSetCurrent != null) txtSetCurrent.Text = "0.00";
                    if (txtActFreq != null) txtActFreq.Text = "0.0";
                    if (txtTime != null) txtTime.Text = "0";
                    if (txtPower != null) txtPower.Text = "0";
                }
            }
            catch
            {
                // ignore
            }
        }

        private static string FormatTime(int seconds)
        {
            if (seconds < 0) seconds = 0;
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

        // Manual control button handlers
        private void OpenCover_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _soakingTank?.ManualCoverClose(false);
            }
            catch
            {
                // ignore
            }
        }

        private void CloseCover_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _soakingTank?.ManualCoverClose(true);
            }
            catch
            {
                // ignore
            }
        }

        private void OpenAir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _soakingTank?.ManualAirOP(true);
            }
            catch
            {
                // ignore
            }
        }

        private void CloseAir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _soakingTank?.ManualAirOP(false);
            }
            catch
            {
                // ignore
            }
        }

        private void OpenWaterIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _soakingTank?.ManualWaterInOP(true);
            }
            catch
            {
                // ignore
            }
        }

        private void CloseWaterIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _soakingTank?.ManualWaterInOP(false);
            }
            catch
            {
                // ignore
            }
        }

        private void OpenUltrasonic_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _soakingTank?.ManualUltrasonicOP(true);
            }
            catch
            {
                // ignore
            }
        }

        private void CloseUltrasonic_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _soakingTank?.ManualUltrasonicOP(false);
            }
            catch
            {
                // ignore
            }
        }

        private void OpenWaterOut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _soakingTank?.ManualWaterOutputOP(true);
            }
            catch
            {
                // ignore
            }
        }

        private void CloseWaterOut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _soakingTank?.ManualWaterOutputOP(false);
            }
            catch
            {
                // ignore
            }
        }
    }
}
