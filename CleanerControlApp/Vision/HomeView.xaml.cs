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
        private const int HoldMilliseconds = 3000; //3 seconds
        private DispatcherTimer? _progressTimer;
        private DateTime _holdStart;
        private Brush? _btnResetOriginalBackground;
        private Style? _btnResetOriginalStyle;

        // New: hardware status poller for buzzer
        private DispatcherTimer? _hwStatusTimer;
        private bool? _lastBuzzerStopState;
        private Brush? _btnStopBuzzerOriginalBackground;
        // track init/system states to update Init button visuals
        private bool? _lastInitializingState;
        private bool? _lastSystemInitializedState;
        // flash timer for Init button when hardware.Initializing == true
        private DispatcherTimer? _initFlashTimer;
        private bool _initFlashState = false;

        private CancellationTokenSource? _initCts;
        private DateTime _initHoldStart;

        // track original Init visuals so we can restore
        private Brush? _btnInitOriginalBackground;
        private Style? _btnInitOriginalStyle;

        // New: cancellation token source for Stop button long press
        private CancellationTokenSource? _stopCts;

        // New: timer to refresh latest operate log periodically
        private DispatcherTimer? _operateLogTimer;

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
            BtnStart.Click += BtnStart_Click;
            BtnPause.Click += BtnPause_Click;
            // Stop: short click -> AutoStop(), long-press3s -> AutoStop(true)
            try
            {
                if (BtnStop != null)
                {
                    BtnStop.Click += (s, e) =>
                    {
                        try
                        {
                            var hw = App.AppHost?.Services?.GetService<HardwareManager>();
                            if (hw == null)
                            {
                                MessageBox.Show("HardwareManager not available.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                            bool stopped = hw.AutoStop();
                            if (!stopped)
                                MessageBox.Show("系統無法停止，可能尚未初始化。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"處理失敗:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };

                    BtnStop.PreviewMouseLeftButtonDown += (s, e) =>
                    {
                        _stopCts?.Cancel();
                        _stopCts = new CancellationTokenSource();
                        var token = _stopCts.Token;

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(HoldMilliseconds, token).ConfigureAwait(false);
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    try
                                    {
                                        var hw = App.AppHost?.Services?.GetService<HardwareManager>();
                                        if (hw == null)
                                        {
                                            MessageBox.Show("HardwareManager not available.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                            return;
                                        }
                                        bool forced = hw.AutoStop(true);
                                        if (forced)
                                            MessageBox.Show("強制停止完成", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                                        else
                                            MessageBox.Show("強制停止失敗", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show($"強制停止失敗:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                });
                            }
                            catch (OperationCanceledException) { }
                            catch { }
                        }, token);
                    };

                    BtnStop.PreviewMouseLeftButtonUp += (s, e) => _stopCts?.Cancel();
                    BtnStop.LostMouseCapture += (s, e) => _stopCts?.Cancel();
                }
            }
            catch { }

            // Subscribe to alarm changes and populate initial UI
            try
            {
                AlarmManager.AlarmsChanged += OnAlarmsChanged;
            }
            catch { }

            try
            {
                PopulateCurrentAlarms();
                LoadLatestOperateLog();
                // populate initial system hint if hardware available
                try
                {
                    var hwInit = App.AppHost?.Services?.GetService<HardwareManager>();
                    if (hwInit != null && tbSystemHint != null)
                    {
                        tbSystemHint.Text = hwInit.Next();
                    }
                }
                catch { }
            }
            catch { }

            // Start hardware status timer for buzzer/init visuals
            try { StartHardwareStatusTimer(); } catch { }

            // Start operate-log refresh timer (updates tbOperateLogLast periodically)
            try
            {
                StartOperateLogTimer();
            }
            catch { }

            // Ensure cleanup on unload
            this.Unloaded += HomeView_Unloaded;

            // Wire hint button
            BtnHint.Click += BtnHint_Click;
        }

        private void StartOperateLogTimer()
        {
            if (_operateLogTimer != null) return;
            _operateLogTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(2) };
            _operateLogTimer.Tick += OperateLogTimer_Tick;
            _operateLogTimer.Start();
        }

        private void StopOperateLogTimer()
        {
            if (_operateLogTimer == null) return;
            _operateLogTimer.Stop();
            _operateLogTimer.Tick -= OperateLogTimer_Tick;
            _operateLogTimer = null;
        }

        private void OperateLogTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // refresh UI with latest operate log entry
                LoadLatestOperateLog();
            }
            catch { }
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

                // Update Init button visuals based on hardware initializing/system initialized state
                bool initializing = hw.Initializing;
                bool systemInit = hw.SystemInitialized;

                // Always update visuals each tick so flashing continues while initializing
                _lastInitializingState = initializing;
                _lastSystemInitializedState = systemInit;
                UpdateInitButtonVisual(initializing, systemInit);
                // update system hint display
                try
                {
                    if (tbSystemHint != null)
                    {
                        tbSystemHint.Text = hw.Next();
                    }
                }
                catch { }
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
                BtnStopBuzzer.Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)); // dark gray
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

        private void UpdateInitButtonVisual(bool initializing, bool systemInitialized)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateInitButtonVisual(initializing, systemInitialized));
                return;
            }

            // If init hold/progress is active, do not override visuals
            if (_initCts != null && !_initCts.IsCancellationRequested)
                return;

            if (systemInitialized)
            {
                // lighter blue background to indicate system initialized (better contrast with black text)
                StopInitFlashTimer();
                BtnInit.Background = new SolidColorBrush(Color.FromRgb(0xB3, 0xD9, 0xFF)); // light blue
                BtnInit.Foreground = Brushes.Black;
            }
            else if (initializing)
            {
                // start flashing light-blue while initializing
                StartInitFlashTimer();
            }
            else
            {
                // restore original visuals
                StopInitFlashTimer();
                if (_btnInitOriginalBackground != null)
                    BtnInit.Background = _btnInitOriginalBackground;
                if (_btnInitOriginalStyle != null)
                    BtnInit.Style = _btnInitOriginalStyle;
                BtnInit.Foreground = Brushes.Black;
            }
        }

        private void StartInitFlashTimer()
        {
            if (_initFlashTimer != null) return;

            // ensure original background captured
            if (_btnInitOriginalBackground == null)
                _btnInitOriginalBackground = BtnInit.Background;

            _initFlashTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(400) };
            _initFlashTimer.Tick += (s, e) =>
            {
                try
                {
                    // do not override visuals if user is holding Init
                    if (_initCts != null && !_initCts.IsCancellationRequested) return;

                    _initFlashState = !_initFlashState;
                    if (_initFlashState)
                    {
                        // light blue flash
                        BtnInit.Background = Brushes.LightBlue;
                        BtnInit.Foreground = Brushes.Black;
                    }
                    else
                    {
                        // restore original background
                        if (_btnInitOriginalBackground != null)
                            BtnInit.Background = _btnInitOriginalBackground;
                        BtnInit.Foreground = Brushes.Black;
                    }
                }
                catch { }
            };
            // set initial flash immediately for responsiveness
            _initFlashState = true;
            BtnInit.Background = Brushes.LightBlue;
            BtnInit.Foreground = Brushes.Black;
            _initFlashTimer.Start();
        }

        private void StopInitFlashTimer()
        {
            if (_initFlashTimer == null) return;
            try
            {
                _initFlashTimer.Stop();
                // no direct handler detach needed for anonymous handler; allow GC to collect
            }
            catch { }
            _initFlashTimer = null;
            _initFlashState = false;
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
                ResetProgressRect.Width = 0;
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
                InitProgressRect.Width = 0;
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
                ResetProgressRect.Width = 0;
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
                    ResetProgressRect.Margin = new Thickness(-leftBorder, 0, 0, 0);
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
                    InitProgressRect.Margin = new Thickness(-initLeftBorder, 0, 0, 0);

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
                MessageBox.Show("錯誤已重置", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void BtnStart_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var hw = App.AppHost?.Services?.GetService<HardwareManager>();
                if (hw == null)
                {
                    MessageBox.Show("HardwareManager not available.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool started = hw.AutoStart();
                if (!started)
                {
                    // AutoStart returned false -> likely system not initialized
                    MessageBox.Show("系統無法啟動，可能尚未初始化。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"啟動失敗:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPause_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var hw = App.AppHost?.Services?.GetService<HardwareManager>();
                if (hw == null)
                {
                    MessageBox.Show("HardwareManager not available.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool paused = hw.AutoPause();
                if (!paused)
                {
                    MessageBox.Show("系統無法暫停，可能尚未初始化。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"暫停失敗:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                InitProgressRect.Width = 0;
            }
            catch { }

            // restore original background
            if (_btnInitOriginalBackground != null)
                BtnInit.Background = _btnInitOriginalBackground;

            // restore original style
            if (_btnInitOriginalStyle != null)
                BtnInit.Style = _btnInitOriginalStyle;

            // stop any flashing when hiding init progress
            StopInitFlashTimer();
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
                    MessageBox.Show("初始化 (Developer)", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // non-developer: perform check first
                    string status = string.Empty;
                    int result = hw.CheckCanInitialize(out status);
                    if (result == 0)
                    {
                        hw.Initialize(false);
                        MessageBox.Show("初始化完成", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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
                if (entries != null && entries.Count > 0)
                {
                    var last = entries.OrderByDescending(x => x.Timestamp).FirstOrDefault();
                    if (last != null)
                    {
                        tbOperateLogLast.Text = $"[{last.Timestamp:yyyy-MM-dd HH:mm:ss}] {last.EventName} by {last.UserName} - {last.Description}";
                        return;
                    }
                }

                tbOperateLogLast.Text = "尚無紀錄";
            }
            catch
            {
                tbOperateLogLast.Text = "讀取 OperateLog 發生錯誤";
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

        private void HomeView_Unloaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                AlarmManager.AlarmsChanged -= OnAlarmsChanged;
                StopHardwareStatusTimer();
                StopOperateLogTimer();
                this.Unloaded -= HomeView_Unloaded;
            }
            catch { }
        }

        private void BtnHint_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var hw = App.AppHost?.Services?.GetService<HardwareManager>();
                if (hw == null)
                {
                    MessageBox.Show("HardwareManager not available.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string hint = hw.Hint();

                // Determine owner window (to center the popup)
                var owner = Window.GetWindow(this);

                // Create content
                var textBlock = new TextBlock
                {
                    Text = hint,
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    Margin = new Thickness(8)
                };

                var scroll = new ScrollViewer
                {
                    Content = textBlock
                };

                var win = new Window()
                {
                    Title = "系統提示",
                    Content = scroll,
                    SizeToContent = SizeToContent.Manual,
                    WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
                };

                if (owner != null)
                {
                    // set owner and size relative to owner (1.5x)
                    win.Owner = owner;

                    double w = owner.ActualWidth >0 ? owner.ActualWidth *1.5 :600 *1.5;
                    double h = owner.ActualHeight >0 ? owner.ActualHeight *1.5 :420 *1.5;

                    // ensure it does not exceed available work area
                    w = Math.Min(w, SystemParameters.WorkArea.Width);
                    h = Math.Min(h, SystemParameters.WorkArea.Height);

                    win.Width = w;
                    win.Height = h;
                }
                else
                {
                    // fallback: scale default size
                    win.Width = 600 * 1.5;
                    win.Height = 420 * 1.5;
                }

                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"顯示Hint失敗:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}