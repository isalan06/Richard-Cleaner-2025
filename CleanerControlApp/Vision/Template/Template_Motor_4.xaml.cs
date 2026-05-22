using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CleanerControlApp.Hardwares.SoakingTank.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_Motor_4.xaml ªº¤¬°ÊÅÞ¿è
    /// </summary>
    public partial class Template_Motor_4 : UserControl, INotifyPropertyChanged
    {
        private readonly ISoakingTank? _soakingTank;
        private readonly DispatcherTimer _timer;

        private bool _limitN;
        private bool _limitP;
        private bool _servoOn;
        private bool _home;
        private bool _idle;
        private bool _alarm;
        private bool _busy;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Template_Motor_4()
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
            UpdateFromSink();
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
            UpdateFromSink();
        }

        private void UpdateFromSink()
        {
            try
            {
                if (_soakingTank != null)
                {
                    LimitN = _soakingTank.MotorUpLimit;
                    LimitP = _soakingTank.MotorDownLimit;
                    ServoOn = _soakingTank.MotorServoOn;
                    // additional status
                    Home = _soakingTank.MotorHome;
                    Idle = _soakingTank.MotorIdle;
                    Alarm = _soakingTank.MotorAlarm;
                    Busy = _soakingTank.MotorBusy;
                    // update position display
                    try
                    {
                        txtPositionValue.Text = _soakingTank.Position_Value.ToString("0.00");
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
                if (_soakingTank != null)
                {
                    var status = _soakingTank.JogStatus;
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
                if (_soakingTank != null)
                {
                    var status = _soakingTank.HomeStatus;
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

                if (_soakingTank == null) return;
                if (sender is Button btn)
                {
                    int dir = 0;
                    if (btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int t)) dir = t;
                    int speed = GetSelectedSpeed();

                    // capture mouse so we can get LostMouseCapture if capture lost
                    try { btn.CaptureMouse(); } catch { }

                    // Start jog
                    _soakingTank.Jog(true, dir, speed);
                }
            }
            catch { }
        }

        private void JogButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_soakingTank == null) return;
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
                if (_soakingTank == null || btn == null) return;
                int dir = 0;
                if (btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int t)) dir = t;
                int speed = GetSelectedSpeed();
                // Stop jog - best effort
                try { _soakingTank.Jog(false, dir, speed); } catch { }
            }
            catch { }
        }

        private void btnServoOn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_soakingTank == null) return;
                // toggle servo state
                _soakingTank.ServoOn(!_soakingTank.MotorServoOn);
                UpdateFromSink();
            }
            catch { }
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_soakingTank == null) return;

                // check shuttle jog status first
                if (!CheckShuttleHomeStatus())
                {
                    e.Handled = true;
                    return;
                }

                _soakingTank.Home();
            }
            catch { }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_soakingTank == null) return;
                _soakingTank.MotorStop();
            }
            catch { }
        }
    }
}
