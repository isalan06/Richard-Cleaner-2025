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
                    if (_shuttle?.ShuttleXMotor != null && _shuttle?.ShuttleZMotor != null && _shuttle.ShuttleXMotor.MotorHome && _shuttle.ShuttleZMotor.MotorHome)
                    {
                        _shuttle?.TeachSemiPosition(idx);
                        try { CleanerControlApp.Vision.Shared.InfoPopup.Show($"Teach (P{idx + 1}) executed.", Window.GetWindow(this), 5); } catch { }
                        //MessageBox.Show($"Teach (P{idx + 1}) executed.", "Teach", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        try
                        {
                            CleanerControlApp.Vision.Shared.StatusPopup.Show($"Cannot teach (P{idx + 1}) Motor not homed.", Window.GetWindow(this), 5);
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    try { CleanerControlApp.Vision.Shared.StatusPopup.Show($"Teach failed: {ex.Message}", Window.GetWindow(this), 5); } catch { }
                    //try { MessageBox.Show($"Teach failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); } catch { }
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
                    ShowInfoPopup($"Semi Pick (P{idx + 1}) started.");
                }
                else
                {
                    string message = _shuttle.MessageForPickPlace;
                    // Use the same popup style as jog failures
                    ShowStatusPopup($"Semi Pick (P{idx + 1}) failed or not started.\r\n{message}");
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
                    ShowInfoPopup($"Semi Place (P{idx + 1}) started.");
                }
                else
                {
                    string message = _shuttle.MessageForPickPlace;
                    // Use the same popup style as jog failures
                    ShowStatusPopup($"Semi Place (P{idx + 1}) failed or not started.\r\n{message}");
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
                CleanerControlApp.Vision.Shared.StatusPopup.Show(status, Window.GetWindow(this), 10);
            }
            catch { }
        }

        private void ShowInfoPopup(string info)
        {
            try
            {
                CleanerControlApp.Vision.Shared.InfoPopup.Show(info, Window.GetWindow(this), 5);
            }
            catch { }
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
