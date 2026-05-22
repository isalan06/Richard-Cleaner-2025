using CleanerControlApp.Hardwares.Shuttle.Interfaces;
using CleanerControlApp.Modules.Motor.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_Motor_2.xaml 的互動邏輯
    /// </summary>
    public partial class Template_Motor_2 : UserControl, INotifyPropertyChanged
    {
        private readonly ISingleAxisMotor? _motor;
        private IShuttle? _shuttle;
        private readonly DispatcherTimer _timer;

        private bool _limitN;
        private bool _limitP;
        private bool _servoOn;
        private bool _home;
        private bool _idle;
        private bool _alarm;
        private bool _busy;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Template_Motor_2()
        {
            InitializeComponent();

            try
            {
                var svc = App.AppHost?.Services.GetService<ISingleAxisMotor[]>();
                if (svc != null)
                    _motor = svc[1];
            }
            catch
            {
                _motor = null;
            }

            try
            {
                _shuttle = App.AppHost?.Services.GetService<IShuttle>();
            }
            catch
            {
                _shuttle = null;
            }

            // set direction tags on buttons
            btnJogPlus.Tag = 0; // JOG + -> dir0
            btnJogMinus.Tag = 1; // JOG - -> dir1

            // attach lost-capture handlers as insurance to always stop jog
            try
            {
                btnJogPlus.LostMouseCapture += JogButton_LostMouseCapture;
                btnJogMinus.LostMouseCapture += JogButton_LostMouseCapture;
            }
            catch { }

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += Timer_Tick;

            Loaded += (s, e) => _timer.Start();
            Unloaded += (s, e) =>
            {
                try { _timer.Stop(); } catch { }
                try { StopJogButton(btnJogPlus); } catch { }
                try { StopJogButton(btnJogMinus); } catch { }
            };

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
                var m = _motor;
                if (m != null)
                {
                    LimitN = m.ErrorLimitN || m.MotorNLimit;
                    LimitP = m.ErrorLimitP || m.MotorPLimit;
                    ServoOn = m.MotorServoOn;
                    // additional status
                    Home = m.MotorHome;
                    Idle = m.MotorIdle;
                    Alarm = m.MotorAlarm;
                    Busy = m.MotorBusy;
                    // update position display
                    try
                    {
                        txtPositionValue.Text = m.Position_Value.ToString("0.00");
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

        // helper to check ShuttleXMotor.JogStatus; returns true if OK to proceed, false if popup shown
        private bool CheckShuttleJogStatus(Point clickScreenPosition)
        {
            try
            {
                if (_shuttle != null && _shuttle.ShuttleZMotor != null)
                {
                    var status = _shuttle.ShuttleZMotor.JogStatus;
                    if (!string.IsNullOrEmpty(status))
                    {
                        ShowStatusPopup(status);
                        return false;
                    }
                }
            }
            catch
            {
                // ignore
            }
            return true;
        }

        private bool CheckShuttleHomeStatus()
        {
            try
            {
                if (_shuttle != null && _shuttle.ShuttleZMotor != null)
                {
                    var status = _shuttle.ShuttleZMotor.HomeStatus;
                    if (!string.IsNullOrEmpty(status))
                    {
                        ShowStatusPopup(status);
                        return false;
                    }
                }
            }
            catch
            {
                // ignore
            }
            return true;
        }

        private void ShowStatusPopup(string status)
        {
            try { CleanerControlApp.Vision.Shared.StatusPopup.Show(status, Window.GetWindow(this),5); } catch { }
        }

        private void JogButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // get mouse position in screen coordinates
                var pos = e.GetPosition(this);
                var screen = PointToScreen(pos);

                // check shuttle jog status first
                if (!CheckShuttleJogStatus(screen))
                {
                    e.Handled = true;
                    return;
                }

                var m = _motor;
                if (m == null) return;
                if (sender is Button btn)
                {
                    int dir = 0;
                    if (btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int t)) dir = t;
                    int speed = GetSelectedSpeed();

                    // capture mouse so we can get LostMouseCapture if capture lost
                    try { btn.CaptureMouse(); } catch { }

                    // Start jog
                    m.Jog(true, dir, speed);
                }
            }
            catch { }
        }

        private void JogButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var m = _motor;
                if (m == null) return;
                if (sender is Button btn)
                {
                    try { if (btn.IsMouseCaptured) btn.ReleaseMouseCapture(); } catch { }
                    StopJogButton(btn);
                }
            }
            catch { }
        }

        // Lost capture: ensure stop called
        private void JogButton_LostMouseCapture(object? sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Button btn)
                {
                    StopJogButton(btn);
                }
            }
            catch { }
        }

        // helper to stop jog for given button safely
        private void StopJogButton(Button? btn)
        {
            try
            {
                if (_motor == null || btn == null) return;
                int dir = 0;
                if (btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int t)) dir = t;
                int speed = GetSelectedSpeed();
                // Stop jog - best effort
                try { _motor.Jog(false, dir, speed); } catch { }
            }
            catch { }
        }

        private void btnServoOn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var m = _motor;
                if (m == null) return;
                // toggle servo state
                m.ServoOn(!m.MotorServoOn);
                UpdateFromMotor();
            }
            catch { }
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var m = _motor;
                if (m == null) return;

                // check shuttle jog status first
                if (!CheckShuttleHomeStatus())
                {
                    e.Handled = true;
                    return;
                }

                m.Home();
            }
            catch { }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var m = _motor;
                if (m == null) return;
                m.MotorStop();
            }
            catch { }
        }
    }
}
