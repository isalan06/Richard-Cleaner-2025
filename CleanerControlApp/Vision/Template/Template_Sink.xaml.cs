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
using CleanerControlApp.Hardwares.Sink.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_Sink.xaml 的互動邏輯
    /// </summary>
    public partial class Template_Sink : UserControl, INotifyPropertyChanged
    {
        private readonly DispatcherTimer _timer;
        private readonly ISink? _sink;

        private bool _highTC;
        private bool _lowTC;
        private double _pv;
        private bool _act;
        private bool _inPos1;
        private bool _inPos2;
        private bool _inPos3;

        // teach long-press timers (one per teach button)
        private readonly DispatcherTimer _teachHoldTimerP1;
        private readonly DispatcherTimer _teachHoldTimerP2;
        private readonly DispatcherTimer _teachHoldTimerP3;
        private bool _teachTriggeredP1 = false;
        private bool _teachTriggeredP2 = false;
        private bool _teachTriggeredP3 = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Template_Sink()
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

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _timer.Tick += Timer_Tick;

            // teach hold timers (3 seconds) - separate timers for each button
            _teachHoldTimerP1 = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(3) };
            _teachHoldTimerP1.Tick += TeachHoldTimerP1_Tick;
            _teachHoldTimerP2 = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(3) };
            _teachHoldTimerP2.Tick += TeachHoldTimerP2_Tick;
            _teachHoldTimerP3 = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(3) };
            _teachHoldTimerP3.Tick += TeachHoldTimerP3_Tick;

            Loaded += (s, e) =>
            {
                DataContext = this;
                _timer.Start();

                // attach long-press handlers for teach buttons if available
                try
                {
                    if (btnTeachP1 != null)
                    {
                        btnTeachP1.PreviewMouseLeftButtonDown += BtnTeachP1_PreviewMouseLeftButtonDown;
                        btnTeachP1.PreviewMouseLeftButtonUp += BtnTeachP1_PreviewMouseLeftButtonUp;
                        btnTeachP1.MouseLeave += BtnTeachP1_MouseLeave;
                    }
                    if (btnTeachP2 != null)
                    {
                        btnTeachP2.PreviewMouseLeftButtonDown += BtnTeachP2_PreviewMouseLeftButtonDown;
                        btnTeachP2.PreviewMouseLeftButtonUp += BtnTeachP2_PreviewMouseLeftButtonUp;
                        btnTeachP2.MouseLeave += BtnTeachP2_MouseLeave;
                    }
                    if (btnTeachP3 != null)
                    {
                        btnTeachP3.PreviewMouseLeftButtonDown += BtnTeachP3_PreviewMouseLeftButtonDown;
                        btnTeachP3.PreviewMouseLeftButtonUp += BtnTeachP3_PreviewMouseLeftButtonUp;
                        btnTeachP3.MouseLeave += BtnTeachP3_MouseLeave;
                    }
                }
                catch { }
            };
            Unloaded += (s, e) =>
            {
                _timer.Stop();
                _teachHoldTimerP1.Stop();
                _teachHoldTimerP2.Stop();
                _teachHoldTimerP3.Stop();
            };
        }

        private void TeachHoldTimerP1_Tick(object? sender, EventArgs e)
        {
            try
            {
                _teachHoldTimerP1.Stop();
                _teachTriggeredP1 = true;

                // visual feedback: restore button background
                try { if (btnTeachP1 != null) btnTeachP1.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }

                // call teach on ISink with position0
                try
                {
                    _sink?.Teach(0);
                    MessageBox.Show("Teach (P1) executed.", "Teach", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    try { MessageBox.Show($"Teach failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); } catch { }
                }
            }
            catch { }
        }

        private void TeachHoldTimerP2_Tick(object? sender, EventArgs e)
        {
            try
            {
                _teachHoldTimerP2.Stop();
                _teachTriggeredP2 = true;

                // visual feedback: restore button background
                try { if (btnTeachP2 != null) btnTeachP2.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }

                // call teach on ISink with position1
                try
                {
                    _sink?.Teach(1);
                    MessageBox.Show("Teach (P2) executed.", "Teach", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    try { MessageBox.Show($"Teach failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); } catch { }
                }
            }
            catch { }
        }

        private void TeachHoldTimerP3_Tick(object? sender, EventArgs e)
        {
            try
            {
                _teachHoldTimerP3.Stop();
                _teachTriggeredP3 = true;

                // visual feedback: restore button background
                try { if (btnTeachP3 != null) btnTeachP3.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }

                // call teach on ISink with position2
                try
                {
                    _sink?.Teach(2);
                    MessageBox.Show("Teach (P3) executed.", "Teach", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    try { MessageBox.Show($"Teach failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); } catch { }
                }
            }
            catch { }
        }

        private void BtnTeachP1_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                _teachTriggeredP1 = false;
                // change background to indicate hold
                try { if (btnTeachP1 != null) btnTeachP1.Background = Brushes.Orange; } catch { }
                _teachHoldTimerP1.Stop();
                _teachHoldTimerP1.Start();
            }
            catch { }
        }

        private void BtnTeachP1_PreviewMouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                _teachHoldTimerP1.Stop();
                // restore background
                try { if (btnTeachP1 != null) btnTeachP1.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }

                // if not triggered, do short-press action (none) or show hint
                if (!_teachTriggeredP1)
                {
                    // optional: show hint that long-press is required
                    try { MessageBox.Show("請長按3 秒以進行 Teach", "提示", MessageBoxButton.OK, MessageBoxImage.Information); } catch { }
                }
            }
            catch { }
        }

        private void BtnTeachP1_MouseLeave(object? sender, MouseEventArgs e)
        {
            try
            {
                _teachHoldTimerP1.Stop();
                try { if (btnTeachP1 != null) btnTeachP1.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }
            }
            catch { }
        }

        private void BtnTeachP2_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                _teachTriggeredP2 = false;
                // change background to indicate hold
                try { if (btnTeachP2 != null) btnTeachP2.Background = Brushes.Orange; } catch { }
                _teachHoldTimerP2.Stop();
                _teachHoldTimerP2.Start();
            }
            catch { }
        }

        private void BtnTeachP2_PreviewMouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                _teachHoldTimerP2.Stop();
                // restore background
                try { if (btnTeachP2 != null) btnTeachP2.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }

                // if not triggered, do short-press action (none) or show hint
                if (!_teachTriggeredP2)
                {
                    // optional: show hint that long-press is required
                    try { MessageBox.Show("請長按3 秒以進行 Teach", "提示", MessageBoxButton.OK, MessageBoxImage.Information); } catch { }
                }
            }
            catch { }
        }

        private void BtnTeachP2_MouseLeave(object? sender, MouseEventArgs e)
        {
            try
            {
                _teachHoldTimerP2.Stop();
                try { if (btnTeachP2 != null) btnTeachP2.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }
            }
            catch { }
        }

        private void BtnTeachP3_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                _teachTriggeredP3 = false;
                // change background to indicate hold
                try { if (btnTeachP3 != null) btnTeachP3.Background = Brushes.Orange; } catch { }
                _teachHoldTimerP3.Stop();
                _teachHoldTimerP3.Start();
            }
            catch { }
        }

        private void BtnTeachP3_PreviewMouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                _teachHoldTimerP3.Stop();
                // restore background
                try { if (btnTeachP3 != null) btnTeachP3.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }

                // if not triggered, do short-press action (none) or show hint
                if (!_teachTriggeredP3)
                {
                    // optional: show hint that long-press is required
                    try { MessageBox.Show("請長按3 秒以進行 Teach", "提示", MessageBoxButton.OK, MessageBoxImage.Information); } catch { }
                }
            }
            catch { }
        }

        private void BtnTeachP3_MouseLeave(object? sender, MouseEventArgs e)
        {
            try
            {
                _teachHoldTimerP3.Stop();
                try { if (btnTeachP3 != null) btnTeachP3.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }
            }
            catch { }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (_sink != null)
                {
                    HighTC = _sink.HighPressure;
                    LowTC = _sink.LowPressure;
                    PV = _sink.PV_Value;
                    Act = _sink.Pressure;
                    InPos1 = _sink.InPos1;
                    InPos2 = _sink.InPos2;
                    InPos3 = _sink.InPos3;
                }
                else
                {
                    HighTC = false;
                    LowTC = false;
                    PV =0.0;
                    Act = false;
                    InPos1 = false;
                    InPos2 = false;
                    InPos3 = false;
                }
            }
            catch
            {
                // ignore
            }
        }

        public bool HighTC
        {
            get => _highTC;
            private set
            {
                if (_highTC != value)
                {
                    _highTC = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool LowTC
        {
            get => _lowTC;
            private set
            {
                if (_lowTC != value)
                {
                    _lowTC = value;
                    OnPropertyChanged();
                }
            }
        }

        public double PV
        {
            get => _pv;
            private set
            {
                if (Math.Abs(_pv - value) >0.0001)
                {
                    _pv = value;
                    OnPropertyChanged();
                }
            }
        }

        // Act maps to ISink.Pressure
        public bool Act
        {
            get => _act;
            private set
            {
                if (_act != value)
                {
                    _act = value;
                    OnPropertyChanged();
                }
            }
        }

        // InPos1 maps to ISink.InPos1
        public bool InPos1
        {
            get => _inPos1;
            private set
            {
                if (_inPos1 != value)
                {
                    _inPos1 = value;
                    OnPropertyChanged();
                }
            }
        }

        // InPos2 maps to ISink.InPos2
        public bool InPos2
        {
            get => _inPos2;
            private set
            {
                if (_inPos2 != value)
                {
                    _inPos2 = value;
                    OnPropertyChanged();
                }
            }
        }

        // InPos3 maps to ISink.InPos3
        public bool InPos3
        {
            get => _inPos3;
            private set
            {
                if (_inPos3 != value)
                {
                    _inPos3 = value;
                    OnPropertyChanged();
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Button click handlers
        private void OpenCover_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _sink?.ManualCoverClose(false);
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
                _sink?.ManualCoverClose(true);
            }
            catch
            {
                // ignore
            }
        }

        private void OpenPressure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _sink?.ManualPressureOP(true);
            }
            catch
            {
                // ignore
            }
        }

        private void ClosePressure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _sink?.ManualPressureOP(false);
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
                _sink?.ManualAirOP(true);
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
                _sink?.ManualAirOP(false);
            }
            catch
            {
                // ignore
            }
        }

        private void ResetAlarm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _sink?.AlarmReset();
            }
            catch
            {
                // ignore
            }
        }

        // Move buttons handlers added to resolve XAML event references
        private int GetSelectedSpeed()
        {
            try
            {
                if (cmbSpeedMode != null && cmbSpeedMode.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    if (int.TryParse(item.Tag.ToString(), out int tag))
                        return tag;
                }
            }
            catch { }
            return 0;
        }

        private void btnMoveToP1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int speed = GetSelectedSpeed();
                _sink?.MoveToPosition(0, speed);
            }
            catch { }
        }

        private void btnMoveToP2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int speed = GetSelectedSpeed();
                _sink?.MoveToPosition(1, speed);
            }
            catch { }
        }

        private void btnMoveToP3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int speed = GetSelectedSpeed();
                _sink?.MoveToPosition(2, speed);
            }
            catch { }
        }

        // Short click handlers for teach buttons (show hint)
        private void btnTeachP1_Click(object sender, RoutedEventArgs e)
        {
            try { MessageBox.Show("請長按3 秒以進行 Teach", "提示", MessageBoxButton.OK, MessageBoxImage.Information); } catch { }
        }
        private void btnTeachP2_Click(object sender, RoutedEventArgs e)
        {
            try { MessageBox.Show("請長按3 秒以進行 Teach", "提示", MessageBoxButton.OK, MessageBoxImage.Information); } catch { }
        }
        private void btnTeachP3_Click(object sender, RoutedEventArgs e)
        {
            try { MessageBox.Show("請長按3 秒以進行 Teach", "提示", MessageBoxButton.OK, MessageBoxImage.Information); } catch { }
        }
    }
}
