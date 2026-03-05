using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CleanerControlApp.Hardwares.DryingTank.Interfacaes;
using Microsoft.Extensions.DependencyInjection;

namespace CleanerControlApp.Vision.Template
{
    public partial class Template_DryingTank : UserControl, INotifyPropertyChanged
    {
        private readonly DispatcherTimer _timer;
        private readonly IDryingTank[]? _dryingTanks;

        public Template_DryingTank()
        {
            InitializeComponent();

            try
            {
                // Resolve drying tanks array from the application's service provider if available
                _dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Template_DryingTank ctor: failed to resolve IDryingTank[] - {ex}");
                // swallow but keep _dryingTanks null so Timer_Tick will skip
            }

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            DataContext = this;
        }

        private void OpenCoverLeft_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 0)
                    _dryingTanks[0].ManualCoverClose(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenCoverLeft_Click exception: {ex}");
            }
        }

        private void CloseCoverLeft_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 0)
                    _dryingTanks[0].ManualCoverClose(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CloseCoverLeft_Click exception: {ex}");
            }
        }

        private void OpenCoverRight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 1)
                {
                    _dryingTanks[1].ManualCoverClose(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenCoverRight_Click exception: {ex}");
            }
        }

        private void CloseCoverRight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 1)
                {
                    _dryingTanks[1].ManualCoverClose(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CloseCoverRight_Click exception: {ex}");
            }
        }

        private void OpenHeaterLeft_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 0)
                    _dryingTanks[0].ManualHeatingOP(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenHeaterLeft_Click exception: {ex}");
            }
        }

        private void CloseHeaterLeft_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 0)
                    _dryingTanks[0].ManualHeatingOP(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CloseHeaterLeft_Click exception: {ex}");
            }
        }

        private void OpenHeaterRight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 1)
                    _dryingTanks[1].ManualHeatingOP(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenHeaterRight_Click exception: {ex}");
            }
        }

        private void CloseHeaterRight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 1)
                    _dryingTanks[1].ManualHeatingOP(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CloseHeaterRight_Click exception: {ex}");
            }
        }

        private void OpenBlowerLeft_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 0)
                    _dryingTanks[0].ManualBlowerOP(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenBlowerLeft_Click exception: {ex}");
            }
        }

        private void CloseBlowerLeft_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 0)
                    _dryingTanks[0].ManualBlowerOP(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CloseBlowerLeft_Click exception: {ex}");
            }
        }

        private void OpenBlowerRight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 1)
                    _dryingTanks[1].ManualBlowerOP(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenBlowerRight_Click exception: {ex}");
            }
        }

        private void CloseBlowerRight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 1)
                    _dryingTanks[1].ManualBlowerOP(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CloseBlowerRight_Click exception: {ex}");
            }
        }

        private void OpenAirLeft_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 0)
                    _dryingTanks[0].ManualAirOP(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenAirLeft_Click exception: {ex}");
            }
        }

        private void CloseAirLeft_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 0)
                    _dryingTanks[0].ManualAirOP(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CloseAirLeft_Click exception: {ex}");
            }
        }

        private void OpenAirRight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 1)
                    _dryingTanks[1].ManualAirOP(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenAirRight_Click exception: {ex}");
            }
        }

        private void CloseAirRight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 1)
                    _dryingTanks[1].ManualAirOP(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CloseAirRight_Click exception: {ex}");
            }
        }

        private void ResetAlarmLeft_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 0)
                {
                    _dryingTanks[0].AlarmReset();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ResetAlarmLeft_Click exception: {ex}");
            }
        }

        private void ResetAlarmRight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length > 1)
                {
                    _dryingTanks[1].AlarmReset();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ResetAlarmRight_Click exception: {ex}");
            }
        }

        // Heating flags for UI
        private bool _heatingLeft;
        public bool HeatingLeft
        {
            get => _heatingLeft;
            set
            {
                if (_heatingLeft != value)
                {
                    _heatingLeft = value;
                    OnPropertyChanged(nameof(HeatingLeft));
                }
            }
        }

        private bool _heatingRight;
        public bool HeatingRight
        {
            get => _heatingRight;
            set
            {
                if (_heatingRight != value)
                {
                    _heatingRight = value;
                    OnPropertyChanged(nameof(HeatingRight));
                }
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (_dryingTanks == null) return;
                if (_dryingTanks.Length > 0)
                {
                    PVLeft = _dryingTanks[0].PV_Value;
                    HighTemperatureLeft = _dryingTanks[0].HighTemperature;
                    LowTemperatureLeft = _dryingTanks[0].LowTemperature;
                    SensorCoverOpenLeft = _dryingTanks[0].Sensor_CoverOpen;
                    SensorCoverCloseLeft = _dryingTanks[0].Sensor_CoverClose;

                    // command states for left tank
                    CommandHeaterAirLeft = _dryingTanks[0].Command_HeaterAirOpen;
                    CommandHeaterBlowerLeft = _dryingTanks[0].Command_HeaterBlower;
                    CommandHeaterCoverCloseLeft = _dryingTanks[0].Command_HeaterCoverClose;

                    // heating state
                    HeatingLeft = _dryingTanks[0].Heating;

                    // elapsed / remaining for left tank (mm:ss)
                    ElapsedHeatingLeft = FormatSeconds(_dryingTanks[0].ElpasedHeatingTime_Seconds);
                    RemainingHeatingLeft = FormatSeconds(_dryingTanks[0].RemainingHeatingTime_Seconds);

                    // status
                    AutoStatusLeft = _dryingTanks[0].Auto;
                    PauseStatusLeft = _dryingTanks[0].Pausing;
                    CassetteStatusLeft = _dryingTanks[0].Cassette;
                    IdleStatusLeft = _dryingTanks[0].Idle;
                    InitializedStatusLeft = _dryingTanks[0].Initialized;
                    WarningStatusLeft = _dryingTanks[0].HasWarning;
                    AlarmStatusLeft = _dryingTanks[0].HasAlarm;

                    // handshake states
                    PickFinishedLeft = _dryingTanks[0].HS_ClamperPickFinished;
                    PlaceFinishedLeft = _dryingTanks[0].HS_ClamperPlaceFinished;
                    MovingLeft = _dryingTanks[0].HS_ClamperMoving;
                    InputPermitLeft = _dryingTanks[0].HS_InputPermit;
                    ActFinishedLeft = _dryingTanks[0].HS_ActFinished;

                }
                if (_dryingTanks.Length > 1)
                {
                    PVRight = _dryingTanks[1].PV_Value;
                    HighTemperatureRight = _dryingTanks[1].HighTemperature;
                    LowTemperatureRight = _dryingTanks[1].LowTemperature;
                    SensorCoverOpenRight = _dryingTanks[1].Sensor_CoverOpen;
                    SensorCoverCloseRight = _dryingTanks[1].Sensor_CoverClose;

                    // command states for right tank
                    CommandHeaterAirRight = _dryingTanks[1].Command_HeaterAirOpen;
                    CommandHeaterBlowerRight = _dryingTanks[1].Command_HeaterBlower;
                    CommandHeaterCoverCloseRight = _dryingTanks[1].Command_HeaterCoverClose;

                    // heating state
                    HeatingRight = _dryingTanks[1].Heating;

                    // elapsed / remaining for right tank (mm:ss)
                    ElapsedHeatingRight = FormatSeconds(_dryingTanks[1].ElpasedHeatingTime_Seconds);
                    RemainingHeatingRight = FormatSeconds(_dryingTanks[1].RemainingHeatingTime_Seconds);

                    // status
                    AutoStatusRight = _dryingTanks[1].Auto;
                    PauseStatusRight = _dryingTanks[1].Pausing;
                    CassetteStatusRight = _dryingTanks[1].Cassette;
                    IdleStatusRight = _dryingTanks[1].Idle;
                    InitializedStatusRight = _dryingTanks[1].Initialized;
                    WarningStatusRight = _dryingTanks[1].HasWarning;
                    AlarmStatusRight = _dryingTanks[1].HasAlarm;

                    // handshake states
                    PickFinishedRight = _dryingTanks[1].HS_ClamperPickFinished;
                    PlaceFinishedRight = _dryingTanks[1].HS_ClamperPlaceFinished;
                    MovingRight = _dryingTanks[1].HS_ClamperMoving;
                    InputPermitRight = _dryingTanks[1].HS_InputPermit;
                    ActFinishedRight = _dryingTanks[1].HS_ActFinished;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Template_DryingTank Timer_Tick exception: {ex}");
                try { _timer.Stop(); } catch { }
            }
        }

        private static string FormatSeconds(int seconds)
        {
            try
            {
                if (seconds < 0) seconds = 0;
                var ts = TimeSpan.FromSeconds(seconds);
                return ts.ToString(@"mm\:ss");
            }
            catch
            {
                return "00:00";
            }
        }

        private float _pvLeft;
        public float PVLeft
        {
            get => _pvLeft;
            set
            {
                if (_pvLeft != value)
                {
                    _pvLeft = value;
                    OnPropertyChanged(nameof(PVLeft));
                }
            }
        }

        private float _pvRight;
        public float PVRight
        {
            get => _pvRight;
            set
            {
                if (_pvRight != value)
                {
                    _pvRight = value;
                    OnPropertyChanged(nameof(PVRight));
                }
            }
        }

        private bool _highTemperatureLeft;
        public bool HighTemperatureLeft
        {
            get => _highTemperatureLeft;
            set
            {
                if (_highTemperatureLeft != value)
                {
                    _highTemperatureLeft = value;
                    OnPropertyChanged(nameof(HighTemperatureLeft));
                }
            }
        }

        private bool _lowTemperatureLeft;
        public bool LowTemperatureLeft
        {
            get => _lowTemperatureLeft;
            set
            {
                if (_lowTemperatureLeft != value)
                {
                    _lowTemperatureLeft = value;
                    OnPropertyChanged(nameof(LowTemperatureLeft));
                }
            }
        }

        private bool _highTemperatureRight;
        public bool HighTemperatureRight
        {
            get => _highTemperatureRight;
            set
            {
                if (_highTemperatureRight != value)
                {
                    _highTemperatureRight = value;
                    OnPropertyChanged(nameof(HighTemperatureRight));
                }
            }
        }

        private bool _lowTemperatureRight;
        public bool LowTemperatureRight
        {
            get => _lowTemperatureRight;
            set
            {
                if (_lowTemperatureRight != value)
                {
                    _lowTemperatureRight = value;
                    OnPropertyChanged(nameof(LowTemperatureRight));
                }
            }
        }

        private bool _sensorCoverOpenLeft;
        public bool SensorCoverOpenLeft
        {
            get => _sensorCoverOpenLeft;
            set
            {
                if (_sensorCoverOpenLeft != value)
                {
                    _sensorCoverOpenLeft = value;
                    OnPropertyChanged(nameof(SensorCoverOpenLeft));
                }
            }
        }

        private bool _sensorCoverCloseLeft;
        public bool SensorCoverCloseLeft
        {
            get => _sensorCoverCloseLeft;
            set
            {
                if (_sensorCoverCloseLeft != value)
                {
                    _sensorCoverCloseLeft = value;
                    OnPropertyChanged(nameof(SensorCoverCloseLeft));
                }
            }
        }

        private bool _sensorCoverOpenRight;
        public bool SensorCoverOpenRight
        {
            get => _sensorCoverOpenRight;
            set
            {
                if (_sensorCoverOpenRight != value)
                {
                    _sensorCoverOpenRight = value;
                    OnPropertyChanged(nameof(SensorCoverOpenRight));
                }
            }
        }

        private bool _sensorCoverCloseRight;
        public bool SensorCoverCloseRight
        {
            get => _sensorCoverCloseRight;
            set
            {
                if (_sensorCoverCloseRight != value)
                {
                    _sensorCoverCloseRight = value;
                    OnPropertyChanged(nameof(SensorCoverCloseRight));
                }
            }
        }

        // Command properties (for Y labels)
        private bool _commandHeaterAirLeft;
        public bool CommandHeaterAirLeft
        {
            get => _commandHeaterAirLeft;
            set
            {
                if (_commandHeaterAirLeft != value)
                {
                    _commandHeaterAirLeft = value;
                    OnPropertyChanged(nameof(CommandHeaterAirLeft));
                }
            }
        }

        private bool _commandHeaterBlowerLeft;
        public bool CommandHeaterBlowerLeft
        {
            get => _commandHeaterBlowerLeft;
            set
            {
                if (_commandHeaterBlowerLeft != value)
                {
                    _commandHeaterBlowerLeft = value;
                    OnPropertyChanged(nameof(CommandHeaterBlowerLeft));
                }
            }
        }

        private bool _commandHeaterCoverCloseLeft;
        public bool CommandHeaterCoverCloseLeft
        {
            get => _commandHeaterCoverCloseLeft;
            set
            {
                if (_commandHeaterCoverCloseLeft != value)
                {
                    _commandHeaterCoverCloseLeft = value;
                    OnPropertyChanged(nameof(CommandHeaterCoverCloseLeft));
                }
            }
        }

        private bool _commandHeaterAirRight;
        public bool CommandHeaterAirRight
        {
            get => _commandHeaterAirRight;
            set
            {
                if (_commandHeaterAirRight != value)
                {
                    _commandHeaterAirRight = value;
                    OnPropertyChanged(nameof(CommandHeaterAirRight));
                }
            }
        }

        private bool _commandHeaterBlowerRight;
        public bool CommandHeaterBlowerRight
        {
            get => _commandHeaterBlowerRight;
            set
            {
                if (_commandHeaterBlowerRight != value)
                {
                    _commandHeaterBlowerRight = value;
                    OnPropertyChanged(nameof(CommandHeaterBlowerRight));
                }
            }
        }

        private bool _commandHeaterCoverCloseRight;
        public bool CommandHeaterCoverCloseRight
        {
            get => _commandHeaterCoverCloseRight;
            set
            {
                if (_commandHeaterCoverCloseRight != value)
                {
                    _commandHeaterCoverCloseRight = value;
                    OnPropertyChanged(nameof(CommandHeaterCoverCloseRight));
                }
            }
        }

        private string _elapsedHeatingLeft = "00:00";
        public string ElapsedHeatingLeft
        {
            get => _elapsedHeatingLeft;
            set
            {
                if (_elapsedHeatingLeft != value)
                {
                    _elapsedHeatingLeft = value;
                    OnPropertyChanged(nameof(ElapsedHeatingLeft));
                }
            }
        }

        private string _remainingHeatingLeft = "00:00";
        public string RemainingHeatingLeft
        {
            get => _remainingHeatingLeft;
            set
            {
                if (_remainingHeatingLeft != value)
                {
                    _remainingHeatingLeft = value;
                    OnPropertyChanged(nameof(RemainingHeatingLeft));
                }
            }
        }

        private string _elapsedHeatingRight = "00:00";
        public string ElapsedHeatingRight
        {
            get => _elapsedHeatingRight;
            set
            {
                if (_elapsedHeatingRight != value)
                {
                    _elapsedHeatingRight = value;
                    OnPropertyChanged(nameof(ElapsedHeatingRight));
                }
            }
        }

        private string _remainingHeatingRight = "00:00";
        public string RemainingHeatingRight
        {
            get => _remainingHeatingRight;
            set
            {
                if (_remainingHeatingRight != value)
                {
                    _remainingHeatingRight = value;
                    OnPropertyChanged(nameof(RemainingHeatingRight));
                }
            }
        }

        private bool _autoStatusLeft = false;
        public bool AutoStatusLeft
        {
            get => _autoStatusLeft;
            set
            {
                if (_autoStatusLeft != value)
                {
                    _autoStatusLeft = value;
                    OnPropertyChanged(nameof(AutoStatusLeft));
                }
            }
        }

        private bool _pauseStatusLeft = false;
        public bool PauseStatusLeft
        {
            get => _pauseStatusLeft;
            set
            {
                if (_pauseStatusLeft != value)
                {
                    _pauseStatusLeft = value;
                    OnPropertyChanged(nameof(PauseStatusLeft));
                }
            }
        }

        private bool _cassetteStatusLeft = false;
        public bool CassetteStatusLeft
        {
            get => _cassetteStatusLeft;
            set
            {
                if (_cassetteStatusLeft != value)
                {
                    _cassetteStatusLeft = value;
                    OnPropertyChanged(nameof(CassetteStatusLeft));
                }
            }
        }

        private bool _idleStatusLeft = false;
        public bool IdleStatusLeft
        {
            get => _idleStatusLeft;
            set
            {
                if (_idleStatusLeft != value)
                {
                    _idleStatusLeft = value;
                    OnPropertyChanged(nameof(IdleStatusLeft));
                }
            }
        }

        private bool _initializedStatusLeft = false;
        public bool InitializedStatusLeft
        {
            get => _initializedStatusLeft;
            set
            {
                if (_initializedStatusLeft != value)
                {
                    _initializedStatusLeft = value;
                    OnPropertyChanged(nameof(InitializedStatusLeft));
                }
            }
        }

        private bool _warningStatusLeft = false;
        public bool WarningStatusLeft
        {
            get => _warningStatusLeft;
            set
            {
                if (_warningStatusLeft != value)
                {
                    _warningStatusLeft = value;
                    OnPropertyChanged(nameof(WarningStatusLeft));
                }
            }
        }

        private bool _alarmStatusLeft = false;
        public bool AlarmStatusLeft
        {
            get => _alarmStatusLeft;
            set
            {
                if (_alarmStatusLeft != value)
                {
                    _alarmStatusLeft = value;
                    OnPropertyChanged(nameof(AlarmStatusLeft));
                }
            }
        }

        private bool _autoStatusRight = false;
        public bool AutoStatusRight
        {
            get => _autoStatusRight;
            set
            {
                if (_autoStatusRight != value)
                {
                    _autoStatusRight = value;
                    OnPropertyChanged(nameof(AutoStatusRight));
                }
            }
        }

        private bool _pauseStatusRight = false;
        public bool PauseStatusRight
        {
            get => _pauseStatusRight;
            set
            {
                if (_pauseStatusRight != value)
                {
                    _pauseStatusRight = value;
                    OnPropertyChanged(nameof(PauseStatusRight));
                }
            }
        }

        private bool _cassetteStatusRight = false;
        public bool CassetteStatusRight
        {
            get => _cassetteStatusRight;
            set
            {
                if (_cassetteStatusRight != value)
                {
                    _cassetteStatusRight = value;
                    OnPropertyChanged(nameof(CassetteStatusRight));
                }
            }
        }

        private bool _idleStatusRight = false;
        public bool IdleStatusRight
        {
            get => _idleStatusRight;
            set
            {
                if (_idleStatusRight != value)
                {
                    _idleStatusRight = value;
                    OnPropertyChanged(nameof(IdleStatusRight));
                }
            }
        }

        private bool _initializedStatusRight = false;
        public bool InitializedStatusRight
        {
            get => _initializedStatusRight;
            set
            {
                if (_initializedStatusRight != value)
                {
                    _initializedStatusRight = value;
                    OnPropertyChanged(nameof(InitializedStatusRight));
                }
            }
        }

        private bool _warningStatusRight = false;
        public bool WarningStatusRight
        {
            get => _warningStatusRight;
            set
            {
                if (_warningStatusRight != value)
                {
                    _warningStatusRight = value;
                    OnPropertyChanged(nameof(WarningStatusRight));
                }
            }
        }

        private bool _alarmStatusRight = false;
        public bool AlarmStatusRight
        {
            get => _alarmStatusRight;
            set
            {
                if (_alarmStatusRight != value)
                {
                    _alarmStatusRight = value;
                    OnPropertyChanged(nameof(AlarmStatusRight));
                }
            }
        }

        private bool _pickFinishedLeft;
        public bool PickFinishedLeft
        {
            get => _pickFinishedLeft;
            set
            {
                if (_pickFinishedLeft != value)
                {
                    _pickFinishedLeft = value;
                    OnPropertyChanged(nameof(PickFinishedLeft));
                }
            }
        }

        private bool _pickFinishedRight;
        public bool PickFinishedRight
        {
            get => _pickFinishedRight;
            set
            {
                if (_pickFinishedRight != value)
                {
                    _pickFinishedRight = value;
                    OnPropertyChanged(nameof(PickFinishedRight));
                }
            }
        }

        private bool _placeFinishedLeft;
        public bool PlaceFinishedLeft
        {
            get => _placeFinishedLeft;
            set
            {
                if (_placeFinishedLeft != value)
                {
                    _placeFinishedLeft = value;
                    OnPropertyChanged(nameof(PlaceFinishedLeft));
                }
            }
        }

        private bool _placeFinishedRight;
        public bool PlaceFinishedRight
        {
            get => _placeFinishedRight;
            set
            {
                if (_placeFinishedRight != value)
                {
                    _placeFinishedRight = value;
                    OnPropertyChanged(nameof(PlaceFinishedRight));
                }
            }
        }

        private bool _movingLeft;
        public bool MovingLeft
        {
            get => _movingLeft;
            set
            {
                if (_movingLeft != value)
                {
                    _movingLeft = value;
                    OnPropertyChanged(nameof(MovingLeft));
                }
            }
        }

        private bool _movingRight;
        public bool MovingRight
        {
            get => _movingRight;
            set
            {
                if (_movingRight != value)
                {
                    _movingRight = value;
                    OnPropertyChanged(nameof(MovingRight));
                }
            }
        }

        private bool _inputPermitLeft;
        public bool InputPermitLeft
        {
            get => _inputPermitLeft;
            set
            {
                if (_inputPermitLeft != value)
                {
                    _inputPermitLeft = value;
                    OnPropertyChanged(nameof(InputPermitLeft));
                }
            }
        }

        private bool _inputPermitRight;
        public bool InputPermitRight
        {
            get => _inputPermitRight;
            set
            {
                if (_inputPermitRight != value)
                {
                    _inputPermitRight = value;
                    OnPropertyChanged(nameof(InputPermitRight));
                }
            }
        }

        private bool _actFinishedLeft;
        public bool ActFinishedLeft
        {
            get => _actFinishedLeft;
            set
            {
                if (_actFinishedLeft != value)
                {
                    _actFinishedLeft = value;
                    OnPropertyChanged(nameof(ActFinishedLeft));
                }
            }
        }

        private bool _actFinishedRight;
        public bool ActFinishedRight
        {
            get => _actFinishedRight;
            set
            {
                if (_actFinishedRight != value)
                {
                    _actFinishedRight = value;
                    OnPropertyChanged(nameof(ActFinishedRight));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
