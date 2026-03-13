using CleanerControlApp.Hardwares.DryingTank.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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


namespace CleanerControlApp.Vision.SystemTemplate
{
    /// <summary>
    /// Template_System_DryingTank2.xaml 的互動邏輯
    /// </summary>
    public partial class Template_System_DryingTank2 : UserControl, INotifyPropertyChanged
    {
        private readonly IDryingTank? _component;
        private readonly DispatcherTimer _timer;

        private bool _autoStatus;
        private bool _pauseStatus;
        private bool _cassetteStatus;
        private bool _idleStatus;
        private bool _initializedStatus;
        private bool _warningStatus;
        private bool _alarmStatus;

        // elapsed / remaining display
        private string _elapsedAct = "00:00";
        private string _remainingAct = "00:00";

        public event PropertyChangedEventHandler? PropertyChanged;
        public Template_System_DryingTank2()
        {
            InitializeComponent();

            try
            {
                var _components = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (_components != null && _components.Length > 1) _component = _components[1];
            }
            catch
            {
                _component = null;
            }

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _timer.Tick += Timer_Tick;

            Loaded += (s, e) => _timer.Start();
            Unloaded += (s, e) => _timer.Stop();

            UpdateFromComponent();
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
            UpdateFromComponent();
        }

        private void UpdateFromComponent()
        {
            try
            {
                if (_component != null)
                {
                    AutoStatus = _component.Auto;
                    PauseStatus = _component.Pausing;
                    CassetteStatus = _component.Cassette;
                    IdleStatus = _component.Idle;
                    InitializedStatus = _component.Initialized;
                    WarningStatus = _component.HasWarning;
                    AlarmStatus = _component.HasAlarm;

                    // elapsed/remaining
                    try
                    {
                        ElapsedAct = FormatTime(_component.ElpasedHeatingTime_Seconds);
                        RemainingAct = FormatTime(_component.RemainingHeatingTime_Seconds);
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
            if (seconds < 0) seconds = 0;
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.ToString(@"mm\:ss");
        }
    }
}
