using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CleanerControlApp.Hardwares.Sink.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_Motor_3.xaml 的互動邏輯
    /// </summary>
    public partial class Template_Motor_3 : UserControl, INotifyPropertyChanged
    {
        private readonly ISink? _sink;
        private readonly DispatcherTimer _timer;

        private bool _limitN;
        private bool _limitP;
        private bool _servoOn;
        private bool _home;
        private bool _idle;
        private bool _alarm;
        private bool _busy;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Template_Motor_3()
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

            // set direction tags on buttons
            btnJogPlus.Tag = 0; // JOG + -> dir0
            btnJogMinus.Tag = 1; // JOG - -> dir1

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += Timer_Tick;

            Loaded += (s, e) => _timer.Start();
            Unloaded += (s, e) => _timer.Stop();

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
                if (_sink != null)
                {
                    LimitN = _sink.MotorUpLimit;
                    LimitP = _sink.MotorDownLimit;
                    ServoOn = _sink.MotorServoOn;
                    // additional status
                    Home = _sink.MotorHome;
                    Idle = _sink.MotorIdle;
                    Alarm = _sink.MotorAlarm;
                    Busy = _sink.MotorBusy;
                    // update position display
                    try
                    {
                        txtPositionValue.Text = _sink.Position_Value.ToString("0.00");
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
                if (_sink == null) return;
                if (sender is Button btn)
                {
                    int dir = 0;
                    if (btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int t)) dir = t;
                    int speed = GetSelectedSpeed();
                    // Start jog
                    _sink.Jog(true, dir, speed);
                }
            }
            catch { }
        }

        private void JogButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_sink == null) return;
                if (sender is Button btn)
                {
                    int dir = 0;
                    if (btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int t)) dir = t;
                    int speed = GetSelectedSpeed();
                    // Stop jog
                    _sink.Jog(false, dir, speed);
                }
            }
            catch { }
        }

        private void btnServoOn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_sink == null) return;
                // toggle servo state
                _sink.ServoOn(!_sink.MotorServoOn);
                UpdateFromSink();
            }
            catch { }
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_sink == null) return;
                _sink.Home();
            }
            catch { }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_sink == null) return;
                _sink.MotorStop();
            }
            catch { }
        }
    }
}
