using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CleanerControlApp.Modules.Motor.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using CleanerControlApp.Hardwares.Shuttle.Interfaces;
using CleanerControlApp.Hardwares.Shuttle.Models;
using System.Windows.Media;

namespace CleanerControlApp.Vision.Manual
{
    /// <summary>
    /// ManualSemiOpView.xaml 的互動邏輯
    /// </summary>
    public partial class ManualSemiOpView : UserControl
    {
        private ISingleAxisMotor[]? _motors;
        private IShuttle? _shuttle;
        private readonly DispatcherTimer _statusTimer;
        // teach long-press timer for semi teach
        private readonly DispatcherTimer _teachHoldTimerSemi;
        private bool _teachTriggeredSemi = false;

        public ManualSemiOpView()
        {
            InitializeComponent();

            try
            {
                _motors = App.AppHost?.Services.GetService<ISingleAxisMotor[]>();
            }
            catch { _motors = null; }

            try
            {
                _shuttle = App.AppHost?.Services.GetService<IShuttle>();
            }
            catch { _shuttle = null; }

            // attach lost-capture handlers to ensure Stop is called
            try
            {
                btnXJogPlus.LostMouseCapture += JogButton_LostMouseCapture;
                btnXJogMinus.LostMouseCapture += JogButton_LostMouseCapture;
                btnZJogPlus.LostMouseCapture += JogButton_LostMouseCapture;
                btnZJogMinus.LostMouseCapture += JogButton_LostMouseCapture;
            }
            catch { }

            // populate position list ComboBox
            try
            {
                cmbSemiPositions.ItemsSource = ShuttleSemiPositionList.Names;
                if (cmbSemiPositions.Items.Count > 0) cmbSemiPositions.SelectedIndex = 0;
            }
            catch { }

            // attach selection changed
            try { cmbSemiPositions.SelectionChanged += CmbSemiPositions_SelectionChanged; } catch { }

            // status timer to refresh定位顯示
            _statusTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(300) };
            _statusTimer.Tick += StatusTimer_Tick;
            Loaded += (s, e) => { try { _statusTimer.Start(); } catch { } };
            Unloaded += (s, e) => { try { _statusTimer.Stop(); } catch { } };

            // teach hold timer (1 second)
            _teachHoldTimerSemi = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(1) };
            _teachHoldTimerSemi.Tick += TeachHoldTimerSemi_Tick;

            // attach long-press handlers for semi teach button
            Loaded += (s, e) =>
            {
                try
                {
                    if (btnTeachSemi != null)
                    {
                        btnTeachSemi.PreviewMouseLeftButtonDown += BtnTeachSemi_PreviewMouseLeftButtonDown;
                        btnTeachSemi.PreviewMouseLeftButtonUp += BtnTeachSemi_PreviewMouseLeftButtonUp;
                        btnTeachSemi.MouseLeave += BtnTeachSemi_MouseLeave;
                    }
                }
                catch { }
            };

            Unloaded += (s, e) => { try { _teachHoldTimerSemi.Stop(); } catch { } };
        }

        // Teach hold timer tick
        private void TeachHoldTimerSemi_Tick(object? sender, EventArgs e)
        {
            try
            {
                _teachHoldTimerSemi.Stop();
                _teachTriggeredSemi = true;
                try { if (btnTeachSemi != null) btnTeachSemi.Background = Brushes.LightSeaGreen; } catch { }

                int idx = cmbSemiPositions?.SelectedIndex ?? -1;
                if (idx < 0) return;
                try
                {
                    _shuttle?.TeachSemiPosition(idx);
                    MessageBox.Show($"Teach (P{idx + 1}) executed.", "Teach", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    try { MessageBox.Show($"Teach failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); } catch { }
                }
            }
            catch { }
        }

        private void BtnTeachSemi_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                _teachTriggeredSemi = false;
                try { if (btnTeachSemi != null) btnTeachSemi.Background = Brushes.Orange; } catch { }
                _teachHoldTimerSemi.Stop();
                _teachHoldTimerSemi.Start();
            }
            catch { }
        }

        private void BtnTeachSemi_PreviewMouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                _teachHoldTimerSemi.Stop();
                try { if (btnTeachSemi != null) btnTeachSemi.Background = Brushes.LightSeaGreen; } catch { }
            }
            catch { }
        }

        private void BtnTeachSemi_MouseLeave(object? sender, MouseEventArgs e)
        {
            try
            {
                _teachHoldTimerSemi.Stop();
                try { if (btnTeachSemi != null) btnTeachSemi.Background = Brushes.LightSeaGreen; } catch { }
            }
            catch { }
        }

        private void StatusTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                UpdateSemiInPosDisplay();
            }
            catch { }
        }

        private void UpdateSemiInPosDisplay()
        {
            try
            {
                if (_shuttle == null || cmbSemiPositions == null || bdSemiInPos == null) return;
                int idx = cmbSemiPositions.SelectedIndex;
                if (idx < 0) return;
                bool inPos = false;
                try { inPos = _shuttle.GetInSemiPosition(idx); } catch { inPos = false; }

                bdSemiInPos.Background = inPos ? Brushes.Lime : Brushes.White;
            }
            catch { }
        }

        private void CmbSemiPositions_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try { UpdateSemiInPosDisplay(); } catch { }
        }

        private void btnTeachSemi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_shuttle == null || cmbSemiPositions == null) return;
                int idx = cmbSemiPositions.SelectedIndex;
                if (idx < 0) return;

                // call TeachSemiPosition on shuttle
                try { _shuttle.TeachSemiPosition(idx); } catch { }

                // refresh display after teaching
                UpdateSemiInPosDisplay();
            }
            catch { }
        }

        private void btnPickSemi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_shuttle == null || cmbSemiPositions == null) return;
                int idx = cmbSemiPositions.SelectedIndex;
                if (idx < 0) return;

                bool ok = false;
                try { ok = _shuttle.SemiPickCassette(idx); } catch { ok = false; }
                if (ok)
                {
                    MessageBox.Show($"Semi Pick (P{idx + 1}) started.", "Pick", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Semi Pick (P{idx + 1}) failed or not started.", "Pick", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch { }
        }

        private void btnPlaceSemi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_shuttle == null || cmbSemiPositions == null) return;
                int idx = cmbSemiPositions.SelectedIndex;
                if (idx < 0) return;

                bool ok = false;
                try { ok = _shuttle.SemiPlaceCassette(idx); } catch { ok = false; }
                if (ok)
                {
                    MessageBox.Show($"Semi Place (P{idx + 1}) started.", "Place", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Semi Place (P{idx + 1}) failed or not started.", "Place", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch { }
        }

        private int GetSelectedSpeed()
        {
            try
            {
                if (cmbJogSpeed_SemiOp?.SelectedItem is ComboBoxItem cbi && cbi.Tag != null)
                {
                    if (int.TryParse(cbi.Tag.ToString(), out int v))
                        return v;
                }
            }
            catch { }
            return 0; // default low
        }

        // helper to check Shuttle motor JogStatus for given module index
        // moduleIndex:0 -> ShuttleXMotor,1 -> ShuttleZMotor
        private bool CheckShuttleJogStatus(Point clickScreenPosition, int moduleIndex)
        {
            try
            {
                if (_shuttle == null) return true;
                ISingleAxisMotor? target = null;
                try
                {
                    if (moduleIndex == 0) target = _shuttle.ShuttleXMotor;
                    else if (moduleIndex == 1) target = _shuttle.ShuttleZMotor;
                }
                catch { target = null; }

                if (target != null)
                {
                    var status = target.JogStatus;
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
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xEE, 0xFF, 0xCC, 0xCC)), // pale red
                    BorderBrush = System.Windows.Media.Brushes.DarkRed,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10)
                };

                var panel = new StackPanel() { Orientation = Orientation.Vertical };
                var txt = new TextBlock() { Text = status, FontSize = 14, TextWrapping = TextWrapping.Wrap, Foreground = System.Windows.Media.Brushes.Black, MaxWidth = 300 };
                panel.Children.Add(txt);

                // countdown text
                int autoCloseSeconds = 5; // auto close after5 seconds
                var countdown = new TextBlock() { Text = $"將在 {autoCloseSeconds} 秒後關閉", FontSize = 12, Margin = new Thickness(0, 8, 0, 0), Foreground = System.Windows.Media.Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center };
                panel.Children.Add(countdown);

                // setup auto-close timer (declare before button so handler can stop it)
                var dt = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(1) };
                int remaining = autoCloseSeconds;
                dt.Tick += (s, e) =>
                {
                    try
                    {
                        remaining -= 1;
                        if (remaining <= 0)
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

                var btn = new Button() { Content = "關閉", FontSize = 18, Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };
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
                if (sender is Button btn)
                {
                    // parse tag as "index,dir" where index is motor index and dir is0/1
                    int motorIndex = 0;
                    int dir = 0;
                    if (btn.Tag != null)
                    {
                        var s = btn.Tag.ToString() ?? string.Empty;
                        var parts = s.Split(',');
                        if (parts.Length >= 2)
                        {
                            int.TryParse(parts[0], out motorIndex);
                            int.TryParse(parts[1], out dir);
                        }
                        else if (parts.Length == 1)
                        {
                            int.TryParse(parts[0], out dir);
                        }
                    }

                    // get mouse position in screen coordinates
                    var pos = e.GetPosition(this);
                    var screen = PointToScreen(pos);

                    // check shuttle jog status first - map motorIndex to module index
                    if (!CheckShuttleJogStatus(screen, motorIndex))
                    {
                        e.Handled = true;
                        return;
                    }

                    int speed = GetSelectedSpeed();

                    // capture mouse
                    try { btn.CaptureMouse(); } catch { }

                    // start jog on specified motor
                    try
                    {
                        if (_motors != null && motorIndex >= 0 && motorIndex < _motors.Length && _motors[motorIndex] != null)
                        {
                            _motors[motorIndex].Jog(true, dir, speed);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void JogButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Button btn)
                {
                    try { if (btn.IsMouseCaptured) btn.ReleaseMouseCapture(); } catch { }
                    StopJogButton(btn);
                }
            }
            catch { }
        }

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

        private void StopJogButton(Button? btn)
        {
            try
            {
                if (btn == null) return;

                int motorIndex = 0;
                int dir = 0;
                if (btn.Tag != null)
                {
                    var s = btn.Tag.ToString() ?? string.Empty;
                    var parts = s.Split(',');
                    if (parts.Length >= 2)
                    {
                        int.TryParse(parts[0], out motorIndex);
                        int.TryParse(parts[1], out dir);
                    }
                    else if (parts.Length == 1)
                    {
                        int.TryParse(parts[0], out dir);
                    }
                }

                int speed = GetSelectedSpeed();

                try
                {
                    if (_motors != null && motorIndex >= 0 && motorIndex < _motors.Length && _motors[motorIndex] != null)
                    {
                        _motors[motorIndex].Jog(false, dir, speed);
                    }
                }
                catch { }
            }
            catch { }
        }
    }
}
