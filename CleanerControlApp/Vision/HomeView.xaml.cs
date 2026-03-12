using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CleanerControlApp.Utilities.Log;
using CleanerControlApp.Utilities.Alarm;
using CleanerControlApp.Hardwares;
using Microsoft.Extensions.DependencyInjection;
using CleanerControlApp.Modules.UserManagement.Services; // for UserManager
using CleanerControlApp.Modules.UserManagement.Models; // add UserRole enum

namespace CleanerControlApp.Vision
{
 /// <summary>
 /// HomeView.xaml 的互動邏輯
 /// </summary>
 public partial class HomeView : UserControl
 {
 private CancellationTokenSource? _resetCts;
 private const int HoldMilliseconds =3000; //3 seconds
 private DispatcherTimer? _progressTimer;
 private DateTime _holdStart;
 private Brush? _btnResetOriginalBackground;
 private Style? _btnResetOriginalStyle;

 // New: hardware status poller for buzzer
 private DispatcherTimer? _hwStatusTimer;
 private bool? _lastBuzzerStopState;
 private Brush? _btnStopBuzzerOriginalBackground;

 private CancellationTokenSource? _initCts;
 private DateTime _initHoldStart;

 // track original Init visuals so we can restore
 private Brush? _btnInitOriginalBackground;
 private Style? _btnInitOriginalStyle;

 public HomeView()
 {
 try
 {
 InitializeComponent();
 }
 catch (Exception ex)
 {
 // Show detailed exception to aid debugging of XAML issues
 MessageBox.Show($"HomeView InitializeComponent failed:\n{ex}", "XAML Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
 throw;
 }

 // capture original button background and style to restore later
 _btnResetOriginalBackground = BtnResetError.Background;
 _btnResetOriginalStyle = BtnResetError.Style;

 // capture init originals
 _btnInitOriginalBackground = BtnInit.Background;
 _btnInitOriginalStyle = BtnInit.Style;

 // capture stop buzzer original background
 _btnStopBuzzerOriginalBackground = BtnStopBuzzer.Background;

 // Wire button handlers
 BtnStopBuzzer.Click += BtnStopBuzzer_Click;

 // Populate initial data
 LoadLatestOperateLog();
 PopulateCurrentAlarms();

 // Subscribe to alarm changes
 AlarmManager.AlarmsChanged += OnAlarmsChanged;

 // Start polling hardware status
 StartHardwareStatusTimer();

 // Unsubscribe when unloaded to avoid leaks
 this.Unloaded += (s, e) =>
 {
 AlarmManager.AlarmsChanged -= OnAlarmsChanged;
 StopHardwareStatusTimer();
 };
 }

 private void StartHardwareStatusTimer()
 {
 if (_hwStatusTimer != null) return;
 _hwStatusTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(500) };
 _hwStatusTimer.Tick += HwStatusTimer_Tick;
 _hwStatusTimer.Start();
 }

 private void StopHardwareStatusTimer()
 {
 if (_hwStatusTimer == null) return;
 _hwStatusTimer.Stop();
 _hwStatusTimer.Tick -= HwStatusTimer_Tick;
 _hwStatusTimer = null;
 }

 private void HwStatusTimer_Tick(object? sender, EventArgs e)
 {
 try
 {
 var hw = App.AppHost?.Services?.GetService<HardwareManager>();
 if (hw == null) return;

 bool buzzerStopped = hw.BuzzerStop;
 if (_lastBuzzerStopState == null || _lastBuzzerStopState.Value != buzzerStopped)
 {
 _lastBuzzerStopState = buzzerStopped;
 UpdateBuzzerButtonVisual(buzzerStopped);
 }
 }
 catch { }
 }

 private void UpdateBuzzerButtonVisual(bool stopped)
 {
 if (!Dispatcher.CheckAccess())
 {
 Dispatcher.Invoke(() => UpdateBuzzerButtonVisual(stopped));
 return;
 }

 if (stopped)
 {
 // dark background to indicate buzzer stopped
 BtnStopBuzzer.Background = new SolidColorBrush(Color.FromRgb(0x33,0x33,0x33)); // dark gray
 BtnStopBuzzer.Foreground = Brushes.White;
 }
 else
 {
 // restore original
 if (_btnStopBuzzerOriginalBackground != null)
 BtnStopBuzzer.Background = _btnStopBuzzerOriginalBackground;
 BtnStopBuzzer.Foreground = Brushes.Black;
 }
 }

 private async void BtnResetError_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
 {
 // start hold timer and progress animation
 _resetCts?.Cancel();
 _resetCts = new CancellationTokenSource();
 var token = _resetCts.Token;

 ShowResetProgress();
 _holdStart = DateTime.Now;
 StartProgressTimer();

 try
 {
 await Task.Run(async () =>
 {
 try
 {
 await Task.Delay(HoldMilliseconds, token).ConfigureAwait(false);
 // if not cancelled, perform reset on UI thread
 Dispatcher.Invoke(async () => await PerformAlarmResetAsync());
 }
 catch (OperationCanceledException) { }
 }, token).ConfigureAwait(false);
 }
 catch { }
 finally
 {
 // hide progress when done or cancelled
 HideResetProgress();
 }
 }

 private void BtnResetError_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
 {
 // cancel hold
 _resetCts?.Cancel();
 StopProgressTimer();
 }

 private void BtnResetError_LostMouseCapture(object sender, MouseEventArgs e)
 {
 _resetCts?.Cancel();
 StopProgressTimer();
 }

 private void ShowResetProgress()
 {
 // Ensure UI thread
 if (!Dispatcher.CheckAccess())
 {
 Dispatcher.Invoke(() => ShowResetProgress());
 return;
 }

 // switch to no-hover style if available to avoid hover color covering progress
 try
 {
 var noHover = TryFindResource("ResetButtonNoHoverStyle") as Style;
 if (noHover != null)
 {
 BtnResetError.Style = noHover;
 }
 }
 catch { }

 // ensure rect is visible and zero width
 try
 {
 ResetProgressRect.Width =0;
 }
 catch { }
 }

 // Added: ShowInitProgress to fix CS0103 when BtnInit_PreviewMouseLeftButtonDown calls it
 private void ShowInitProgress()
 {
 // Ensure UI thread
 if (!Dispatcher.CheckAccess())
 {
 Dispatcher.Invoke(() => ShowInitProgress());
 return;
 }

 // switch to no-hover style if available to avoid hover color covering progress
 try
 {
 var noHover = TryFindResource("ResetButtonNoHoverStyle") as Style;
 if (noHover != null)
 {
 BtnInit.Style = noHover;
 }
 }
 catch { }

 // ensure rect is visible and zero width
 try
 {
 InitProgressRect.Width =0;
 }
 catch { }
 }

 private void HideResetProgress()
 {
 // Ensure UI thread
 if (!Dispatcher.CheckAccess())
 {
 Dispatcher.Invoke(() => HideResetProgress());
 return;
 }

 // reset rect
 try
 {
 ResetProgressRect.Width =0;
 }
 catch { }

 // restore original background
 if (_btnResetOriginalBackground != null)
 BtnResetError.Background = _btnResetOriginalBackground;

 // restore original style
 if (_btnResetOriginalStyle != null)
 BtnResetError.Style = _btnResetOriginalStyle;

 StopProgressTimer();
 }

 private void StartProgressTimer()
 {
 if (_progressTimer != null) return;
 _progressTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(40) };
 _progressTimer.Tick += ProgressTimer_Tick;
 _progressTimer.Start();
 }

 private void StopProgressTimer()
 {
 if (_progressTimer == null) return;
 _progressTimer.Stop();
 _progressTimer.Tick -= ProgressTimer_Tick;
 _progressTimer = null;

 // reset background and style when stopping prematurely
 if (!Dispatcher.CheckAccess())
 {
 Dispatcher.Invoke(() =>
 {
 if (_btnResetOriginalBackground != null)
 BtnResetError.Background = _btnResetOriginalBackground;
 if (_btnResetOriginalStyle != null)
 BtnResetError.Style = _btnResetOriginalStyle;
 });
 }
 else
 {
 if (_btnResetOriginalBackground != null)
 BtnResetError.Background = _btnResetOriginalBackground;
 if (_btnResetOriginalStyle != null)
 BtnResetError.Style = _btnResetOriginalStyle;
 }

 // also restore Init visuals if they were changed
 if (!Dispatcher.CheckAccess())
 {
 Dispatcher.Invoke(() =>
 {
 if (_btnInitOriginalBackground != null)
 BtnInit.Background = _btnInitOriginalBackground;
 if (_btnInitOriginalStyle != null)
 BtnInit.Style = _btnInitOriginalStyle;
 });
 }
 else
 {
 if (_btnInitOriginalBackground != null)
 BtnInit.Background = _btnInitOriginalBackground;
 if (_btnInitOriginalStyle != null)
 BtnInit.Style = _btnInitOriginalStyle;
 }
 }

 private void ProgressTimer_Tick(object? sender, EventArgs e)
 {
 // Use UI thread immediately
 if (!Dispatcher.CheckAccess())
 {
 Dispatcher.Invoke(() => ProgressTimer_Tick(sender, e));
 return;
 }

 try
 {
 // Update Reset button progress only when reset hold is active
 if (_resetCts != null && !_resetCts.IsCancellationRequested)
 {
 var elapsed = (DateTime.Now - _holdStart).TotalMilliseconds;
 var progress = Math.Max(0, Math.Min(1, elapsed / (double)HoldMilliseconds));

 // make reset button background transparent so rectangle is visible
 BtnResetError.Background = Brushes.Transparent;

 double full = BtnResetError.ActualWidth; // full control width including borders
 double newWidth = full * progress;
 var leftBorder = BtnResetError.BorderThickness.Left;
 ResetProgressRect.Width = newWidth;
 ResetProgressRect.Margin = new Thickness(-leftBorder,0,0,0);
 }

 // Update Init button progress only when init hold is active
 if (_initCts != null && !_initCts.IsCancellationRequested)
 {
 double initElapsed = (DateTime.Now - _initHoldStart).TotalMilliseconds;
 var initProgress = Math.Max(0, Math.Min(1, initElapsed / (double)HoldMilliseconds));

 // make Init background transparent so rectangle visible and apply no-hover style
 BtnInit.Background = Brushes.Transparent;
 double initFull = BtnInit.ActualWidth;
 double initNewWidth = initFull * initProgress;
 var initLeftBorder = BtnInit.BorderThickness.Left;
 InitProgressRect.Width = initNewWidth;
 InitProgressRect.Margin = new Thickness(-initLeftBorder,0,0,0);

 try
 {
 var noHover = TryFindResource("ResetButtonNoHoverStyle") as Style;
 if (noHover != null)
 {
 BtnInit.Style = noHover;
 }
 }
 catch { }
 }
 }
 catch { }
 }

 private void UpdateResetButtonProgress(double progress)
 {
 // Ensure UI thread
 if (!Dispatcher.CheckAccess())
 {
 Dispatcher.Invoke(() => UpdateResetButtonProgress(progress));
 return;
 }

 // make button background transparent so rectangle is visible
 BtnResetError.Background = Brushes.Transparent;
 }

 private async Task PerformAlarmResetAsync()
 {
 try
 {
 var hw = App.AppHost?.Services?.GetService<HardwareManager>();
 if (hw == null)
 {
 MessageBox.Show("HardwareManager not available.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
 return;
 }
 await hw.AlarmResetAsync().ConfigureAwait(false);
 MessageBox.Show("錯誤重置已執行", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
 }
 catch (Exception ex)
 {
 MessageBox.Show($"錯誤重置失敗:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }

 private void BtnStopBuzzer_Click(object? sender, RoutedEventArgs e)
 {
 try
 {
 var hw = App.AppHost?.Services?.GetService<HardwareManager>();
 if (hw == null)
 {
 MessageBox.Show("HardwareManager not available.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
 return;
 }
 hw.Buzzer_Stop();
 }
 catch (Exception ex)
 {
 MessageBox.Show($"停止蜂鳴器失敗:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }

 private async void BtnInit_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
 {
 _initCts?.Cancel();
 _initCts = new CancellationTokenSource();
 var token = _initCts.Token;
 _initHoldStart = DateTime.Now;

 // start visuals for init without touching reset start
 ShowInitProgress();
 StartProgressTimer();

 try
 {
 await Task.Run(async () =>
 {
 try
 {
 await Task.Delay(HoldMilliseconds, token).ConfigureAwait(false);
 // after hold elapsed, perform initialization logic on UI thread
 await Dispatcher.InvokeAsync(async () =>
 {
 try
 {
 await PerformInitializeAsync();
 }
 finally
 {
 // ensure init visuals are hidden after execution
 HideInitProgress();
 // if reset is not active, stop shared timer
 if (_resetCts == null || _resetCts.IsCancellationRequested)
 {
 StopProgressTimer();
 }
 }
 });
 }
 catch (OperationCanceledException) { }
 }, token).ConfigureAwait(false);
 }
 catch { }
 finally
 {
 // nothing here; visuals handled above or on cancel
 }
 }

 private void BtnInit_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
 {
 // cancel hold
 _initCts?.Cancel();
 // hide init visuals
 HideInitProgress();
 // if reset not active, stop shared timer
 if (_resetCts == null || _resetCts.IsCancellationRequested)
 {
 StopProgressTimer();
 }
 }

 private void BtnInit_LostMouseCapture(object sender, MouseEventArgs e)
 {
 _initCts?.Cancel();
 HideInitProgress();
 if (_resetCts == null || _resetCts.IsCancellationRequested)
 {
 StopProgressTimer();
 }
 }

 private void HideInitProgress()
 {
 // Ensure UI thread
 if (!Dispatcher.CheckAccess())
 {
 Dispatcher.Invoke(() => HideInitProgress());
 return;
 }

 try
 {
 InitProgressRect.Width =0;
 }
 catch { }

 // restore original background
 if (_btnInitOriginalBackground != null)
 BtnInit.Background = _btnInitOriginalBackground;

 // restore original style
 if (_btnInitOriginalStyle != null)
 BtnInit.Style = _btnInitOriginalStyle;
 }

 private async Task PerformInitializeAsync()
 {
 try
 {
 var hw = App.AppHost?.Services?.GetService<HardwareManager>();
 if (hw == null)
 {
 MessageBox.Show("HardwareManager not available.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
 return;
 }

 // check user role
 var role = UserManager.CurrentUserRole;
 if (role == UserRole.Developer)
 {
 // developer: force initialize
 hw.Initialize(true);
 MessageBox.Show("已強制初始化(Developer)", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
 }
 else
 {
 // non-developer: perform check first
 string status = string.Empty;
 int result = hw.CheckCanInitialize(out status);
 if (result ==0)
 {
 hw.Initialize(false);
 MessageBox.Show("已初始化", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
 }
 else
 {
 MessageBox.Show(status, "Initialization Check", MessageBoxButton.OK, MessageBoxImage.Warning);
 }
 }
 }
 catch (Exception ex)
 {
 MessageBox.Show($"初始化失敗:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }

 private void LoadLatestOperateLog()
 {
 try
 {
 var entries = OperateLog.GetEntriesForDate(DateTime.Now);
 if (entries != null && entries.Count >0)
 {
 var last = entries.OrderByDescending(x => x.Timestamp).FirstOrDefault();
 if (last != null)
 {
 tbOperateLogLast.Text = $"[{last.Timestamp:yyyy-MM-dd HH:mm:ss}] {last.EventName} by {last.UserName} - {last.Description}";
 return;
 }
 }

 tbOperateLogLast.Text = "尚無資料";
 }
 catch
 {
 tbOperateLogLast.Text = "讀取 OperateLog 時發生錯誤";
 }
 }

 private void PopulateCurrentAlarms()
 {
 try
 {
 // Ensure we run on UI thread
 if (!Dispatcher.CheckAccess())
 {
 Dispatcher.Invoke(() => PopulateCurrentAlarms());
 return;
 }

 var items = AlarmManager.GetAllEntries()
 .Where(a => a.IsAlarm)
 .OrderByDescending(a => a.HappenTime)
 .ToList();

 // Set AlarmEntry objects as ItemsSource so XAML bindings (Type, Code, Description, HappenTime) work
 lbCurrentAlarms.ItemsSource = items;
 }
 catch
 {
 // ignore populate errors
 }
 }

 private void OnAlarmsChanged()
 {
 // AlarmManager may call this from non-UI thread, marshal to UI
 if (!Dispatcher.CheckAccess())
 {
 Dispatcher.Invoke(PopulateCurrentAlarms);
 }
 else
 {
 PopulateCurrentAlarms();
 }
 }
 }
}