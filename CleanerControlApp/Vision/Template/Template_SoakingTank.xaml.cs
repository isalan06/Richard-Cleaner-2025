using System;
using System.Windows;
using System.Windows.Controls;
using CleanerControlApp.Hardwares.SoakingTank.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace CleanerControlApp.Vision.Template
{
 /// <summary>
 /// Template_SoakingTank.xaml 的互動邏輯
 /// </summary>
 public partial class Template_SoakingTank : UserControl, INotifyPropertyChanged
 {
 private readonly ISoakingTank? _soakingTank;
 private readonly DispatcherTimer _timer;

 // teach long-press timers
 private readonly DispatcherTimer _teachHoldTimerP1;
 private readonly DispatcherTimer _teachHoldTimerP2;
 private readonly DispatcherTimer _teachHoldTimerP3;
 private bool _teachTriggeredP1 = false;
 private bool _teachTriggeredP2 = false;
 private bool _teachTriggeredP3 = false;

 private bool _inPos1;
 private bool _inPos2;
 private bool _inPos3;

 public event PropertyChangedEventHandler? PropertyChanged;

 public Template_SoakingTank()
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

 _timer = new DispatcherTimer(DispatcherPriority.Normal)
 {
 Interval = TimeSpan.FromMilliseconds(250)
 };
 _timer.Tick += Timer_Tick;

 // teach hold timers (3 seconds)
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
 try { if (btnTeachP1 != null) btnTeachP1.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }

 try
 {
 _soakingTank?.Teach(0);
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
 try { if (btnTeachP2 != null) btnTeachP2.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }

 try
 {
 _soakingTank?.Teach(1);
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
 try { if (btnTeachP3 != null) btnTeachP3.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }

 try
 {
 _soakingTank?.Teach(2);
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
 try { if (btnTeachP1 != null) btnTeachP1.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }

 if (!_teachTriggeredP1)
 {
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
 try { if (btnTeachP2 != null) btnTeachP2.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }

 if (!_teachTriggeredP2)
 {
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
 try { if (btnTeachP3 != null) btnTeachP3.Background = new SolidColorBrush(Color.FromRgb(0xAD,0xD8,0xE6)); } catch { }

 if (!_teachTriggeredP3)
 {
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
 if (_soakingTank != null)
 {
 InPos1 = _soakingTank.InPos1;
 InPos2 = _soakingTank.InPos2;
 InPos3 = _soakingTank.InPos3;
 }
 else
 {
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

 // Button click handlers for motor functions
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
 _soakingTank?.MoveToPosition(0, speed);
 }
 catch { }
 }

 private void btnMoveToP2_Click(object sender, RoutedEventArgs e)
 {
 try
 {
 int speed = GetSelectedSpeed();
 _soakingTank?.MoveToPosition(1, speed);
 }
 catch { }
 }

 private void btnMoveToP3_Click(object sender, RoutedEventArgs e)
 {
 try
 {
 int speed = GetSelectedSpeed();
 _soakingTank?.MoveToPosition(2, speed);
 }
 catch { }
 }

 // Teach button short-click handlers (show hint)
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

 // existing control handlers for cover/air/water/etc. are preserved
 private void OpenCover_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualCoverClose(false); } catch { }
 }

 private void CloseCover_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualCoverClose(true); } catch { }
 }

 private void OpenAir_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualAirOP(true); } catch { }
 }

 private void CloseAir_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualAirOP(false); } catch { }
 }

 private void OpenWaterIn_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualWaterInOP(true); } catch { }
 }

 private void CloseWaterIn_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualWaterInOP(false); } catch { }
 }

 private void OpenUltrasonic_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualUltrasonicOP(true); } catch { }
 }

 private void CloseUltrasonic_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualUltrasonicOP(false); } catch { }
 }

 private void OpenWaterOut_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualWaterOutputOP(true); } catch { }
 }

 private void CloseWaterOut_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualWaterOutputOP(false); } catch { }
 }

 // Reset alarm button handler
 private void ResetAlarm_Click(object sender, RoutedEventArgs e)
 {
 try
 {
 _soakingTank?.AlarmReset();
 }
 catch { }
 }
 }
}
