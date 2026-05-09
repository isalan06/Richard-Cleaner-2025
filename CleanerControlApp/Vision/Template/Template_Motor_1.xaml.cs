using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CleanerControlApp.Modules.Motor.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CleanerControlApp.Hardwares.Shuttle.Interfaces;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_Motor_1.xaml 的互動邏輯
    /// </summary>
    public partial class Template_Motor_1 : UserControl, INotifyPropertyChanged
    {
        private ISingleAxisMotor? _motor; // originally direct field
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

            try
            {
                _shuttle = App.AppHost?.Services.GetService<IShuttle>();
            }
            catch
            {
                _shuttle = null;
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

        // helper to check ShuttleXMotor.JogStatus; returns true if OK to proceed, false if popup shown
        private bool CheckShuttleJogStatus(Point clickScreenPosition)
        {
            try
            {
                if (_shuttle != null && _shuttle.ShuttleXMotor != null)
                {
                    var status = _shuttle.ShuttleXMotor.JogStatus;
                    if (!string.IsNullOrEmpty(status))
                    {
                        ShowStatusPopup(status, clickScreenPosition);
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

        private void ShowStatusPopup(string status, Point screenPosition)
        {
            try
            {
                var owner = Window.GetWindow(this);
                var w = new Window()
                {
                    Title = "無法操作原因",
                    Owner = owner,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    ShowInTaskbar = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var border = new Border()
                {
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xEE,0xFF,0xCC,0xCC)), // pale red
                    BorderBrush = System.Windows.Media.Brushes.DarkRed,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10)
                };

                var panel = new StackPanel() { Orientation = Orientation.Vertical };
                var txt = new TextBlock() { Text = status, FontSize =14, TextWrapping = TextWrapping.Wrap, Foreground = System.Windows.Media.Brushes.Black, MaxWidth =300 };
                panel.Children.Add(txt);

                // countdown text
                int autoCloseSeconds =5; // auto close after5 seconds
                var countdown = new TextBlock() { Text = $"將在 {autoCloseSeconds} 秒後關閉", FontSize =12, Margin = new Thickness(0,8,0,0), Foreground = System.Windows.Media.Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center };
                panel.Children.Add(countdown);

                // setup auto-close timer (declare before button so handler can stop it)
                var dt = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(1) };
                int remaining = autoCloseSeconds;
                dt.Tick += (s, e) =>
                {
                    try
                    {
                        remaining -=1;
                        if (remaining <=0)
                        {
                            dt.Stop();
                            try { if (w.IsVisible) w.Close(); } catch { }
                        }
                        else
                        {
                            try { countdown.Text = $"將在 {remaining} 秒後關閉"; } catch { }
                        }
                    }
                    catch { }
                };

                var btn = new Button() { Content = "關閉", FontSize =18, Padding = new Thickness(8,6,8,6), Margin = new Thickness(0,10,0,0), HorizontalAlignment = HorizontalAlignment.Center };
                btn.Click += (s, e) =>
                {
                    try
                    {
                        if (dt.IsEnabled) dt.Stop();
                        // close immediately on UI thread
                        try { w.Close(); } catch { }
                    }
                    catch { }
                };
                panel.Children.Add(btn);

                border.Child = panel;
                w.Content = border;

                // stop timer if window closed by other means
                w.Closed += (s, e) => { try { if (dt.IsEnabled) dt.Stop(); } catch { } };

                w.Loaded += (s, e) =>
                {
                    try
                    {
                        // start countdown after window shown
                        dt.Start();
                    }
                    catch { }
                };

                // show as modal dialog
                try { w.ShowDialog(); } catch { }
            }
            catch
            {
                // ignore
            }
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
                var pos = e.GetPosition(this);
                var screen = PointToScreen(pos);

                // check shuttle jog status first
                if (!CheckShuttleJogStatus(screen))
                {
                    e.Handled = true;
                    return;
                }

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
