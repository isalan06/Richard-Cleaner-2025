using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CleanerControlApp.Modules.Motor.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_Motor_1.xaml 的互動邏輯
    /// </summary>
    public partial class Template_Motor_1 : UserControl, INotifyPropertyChanged
    {
        private ISingleAxisMotor? _motor; // originally direct field
        private readonly DispatcherTimer _timer;

        private bool _limitN;
        private bool _limitP;
        private bool _servoOn;
        private bool _home;
        private bool _idle;
        private bool _alarm;
        private bool _busy;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Template_Motor_1()
        {
            InitializeComponent();

            try
            {
                // read from DI the first available ISingleAxisMotor (index0)
                var svc = App.AppHost?.Services.GetService<ISingleAxisMotor[]>();
                if (svc != null)
                {
                    _motor = svc[0];
                }
            }
            catch
            {
                _motor = null;
            }

            // set direction tags on buttons
            btnJogPlus.Tag =0; // JOG + -> dir0
            btnJogMinus.Tag =1; // JOG - -> dir1

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += Timer_Tick;

            Loaded += (s, e) => _timer.Start();
            Unloaded += (s, e) => _timer.Stop();

            // initial read
            UpdateFromMotor();
        }

        public bool LimitN
        {
            get => _limitN;
            private set
            {
                if (_limitN != value)
                {
                    _limitN = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool LimitP
        {
            get => _limitP;
            private set
            {
                if (_limitP != value)
                {
                    _limitP = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ServoOn
        {
            get => _servoOn;
            private set
            {
                if (_servoOn != value)
                {
                    _servoOn = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Home
        {
            get => _home;
            private set
            {
                if (_home != value)
                {
                    _home = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Idle
        {
            get => _idle;
            private set
            {
                if (_idle != value)
                {
                    _idle = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Alarm
        {
            get => _alarm;
            private set
            {
                if (_alarm != value)
                {
                    _alarm = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Busy
        {
            get => _busy;
            private set
            {
                if (_busy != value)
                {
                    _busy = value;
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
            UpdateFromMotor();
        }

        private void UpdateFromMotor()
        {
            try
            {
                if (_motor != null)
                {
                    LimitN = _motor.ErrorLimitN || _motor.MotorNLimit;
                    LimitP = _motor.ErrorLimitP || _motor.MotorPLimit;
                    ServoOn = _motor.MotorServoOn;
                    // additional status
                    Home = _motor.MotorHome;
                    Idle = _motor.MotorIdle;
                    Alarm = _motor.MotorAlarm;
                    Busy = _motor.MotorBusy;
                    // update position display
                    try
                    {
                        txtPositionValue.Text = _motor.Position_Value.ToString("0.00");
                    }
                    catch
                    {
                        txtPositionValue.Text = "0.00";
                    }
                }
                else
                {
                    LimitN = false;
                    LimitP = false;
                    ServoOn = false;
                    Home = false;
                    Idle = false;
                    Alarm = false;
                    Busy = false;
                    txtPositionValue.Text = "0.00";
                }
            }
            catch
            {
                // ignore
            }
        }

        private int GetSelectedSpeed()
        {
            try
            {
                if (cmbJogSpeed?.SelectedItem is ComboBoxItem cbi && cbi.Tag != null)
                {
                    if (int.TryParse(cbi.Tag.ToString(), out int v))
                        return v;
                }
            }
            catch { }
            return 0; // default low
        }

        private void JogButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_motor == null) return;
                if (sender is Button btn)
                {
                    int dir =0;
                    if (btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int t)) dir = t;
                    int speed = GetSelectedSpeed();
                    // Start jog
                    _motor.Jog(true, dir, speed);
                }
            }
            catch { }
        }

        private void JogButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_motor == null) return;
                if (sender is Button btn)
                {
                    int dir =0;
                    if (btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int t)) dir = t;
                    int speed = GetSelectedSpeed();
                    // Stop jog
                    _motor.Jog(false, dir, speed);
                }
            }
            catch { }
        }

        private void btnServoOn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_motor == null) return;
                // toggle servo state
                _motor.ServoOn(!_motor.MotorServoOn);
                UpdateFromMotor();
            }
            catch { }
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_motor == null) return;
                _motor.Home();
            }
            catch { }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_motor == null) return;
                _motor.MotorStop();
            }
            catch { }
        }
    }
}
