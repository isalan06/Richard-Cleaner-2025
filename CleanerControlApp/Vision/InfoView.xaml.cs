using System;
using System.Reflection;
using System.Windows.Controls;
using CleanerControlApp.Utilities.Log;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Diagnostics;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Threading;
using CleanerControlApp.Hardwares;

namespace CleanerControlApp.Vision
{
    /// <summary>
    /// InfoView.xaml 的互動邏輯
    /// </summary>
    public partial class InfoView : UserControl, INotifyPropertyChanged
    {
        private string? _lastLoadedLogFile;

        // Communication status brushes (bound in XAML)
        private Brush _modbusTcpBrush = Brushes.Red;
        private Brush _modbusRtu1Brush = Brushes.Red;
        private Brush _modbusRtu2Brush = Brushes.Red;
        private Brush _modbusRtu3Brush = Brushes.Red;
        private Brush _modbusRtu4Brush = Brushes.Red;

        public Brush ModbusTcpBrush { get => _modbusTcpBrush; set { _modbusTcpBrush = value; RaisePropertyChanged(nameof(ModbusTcpBrush)); } }
        public Brush ModbusRtu1Brush { get => _modbusRtu1Brush; set { _modbusRtu1Brush = value; RaisePropertyChanged(nameof(ModbusRtu1Brush)); } }
        public Brush ModbusRtu2Brush { get => _modbusRtu2Brush; set { _modbusRtu2Brush = value; RaisePropertyChanged(nameof(ModbusRtu2Brush)); } }
        public Brush ModbusRtu3Brush { get => _modbusRtu3Brush; set { _modbusRtu3Brush = value; RaisePropertyChanged(nameof(ModbusRtu3Brush)); } }
        public Brush ModbusRtu4Brush { get => _modbusRtu4Brush; set { _modbusRtu4Brush = value; RaisePropertyChanged(nameof(ModbusRtu4Brush)); } }

        private readonly DispatcherTimer _statusTimer;
        private readonly HardwareManager? _hardwareManager;

        public InfoView()
        {
            InitializeComponent();

            // set DataContext for bindings in XAML
            DataContext = this;

            LoadSystemInfo();
            dpLogDate.SelectedDate = DateTime.Today;
            LoadLogForDate(DateTime.Today);

            // resolve HardwareManager from DI host if available
            _hardwareManager = App.AppHost?.Services.GetService(typeof(HardwareManager)) as HardwareManager;

            // timer to update comm status
            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _statusTimer.Tick += (s, e) => UpdateCommStatus();
            _statusTimer.Start();

            this.Unloaded += InfoView_Unloaded;
        }

        private void InfoView_Unloaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                _statusTimer.Stop();
                this.Unloaded -= InfoView_Unloaded;
            }
            catch { }
        }

        private void UpdateCommStatus()
        {
            try
            {
                // try to obtain manager if it wasn't available at construction
                if (_hardwareManager == null && App.AppHost != null)
                {
                    var hm = App.AppHost.Services.GetService(typeof(HardwareManager)) as HardwareManager;
                    if (hm != null)
                    {
                        // assign via reflection to private readonly field isn't possible here; so just use local variable path
                    }
                }

                var hmLocal = App.AppHost?.Services.GetService(typeof(HardwareManager)) as HardwareManager ?? _hardwareManager;
                if (hmLocal != null)
                {
                    ModbusTcpBrush = hmLocal.ModbusTCPConnected ? Brushes.Lime : Brushes.Red;
                    ModbusRtu1Brush = hmLocal.ModbusRTU1Connected ? Brushes.Lime : Brushes.Red;
                    ModbusRtu2Brush = hmLocal.ModbusRTU2Connected ? Brushes.Lime : Brushes.Red;
                    ModbusRtu3Brush = hmLocal.ModbusRTU3Connected ? Brushes.Lime : Brushes.Red;
                    ModbusRtu4Brush = hmLocal.ModbusRTU4Connected ? Brushes.Lime : Brushes.Red;
                }
                else
                {
                    ModbusTcpBrush = Brushes.Red;
                    ModbusRtu1Brush = Brushes.Red;
                    ModbusRtu2Brush = Brushes.Red;
                    ModbusRtu3Brush = Brushes.Red;
                    ModbusRtu4Brush = Brushes.Red;
                }
            }
            catch { }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        private void RaisePropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

        private void LoadSystemInfo()
        {
            try
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

                // Prefer AssemblyInformationalVersion if present
                var infoAttr = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var version = infoAttr?.InformationalVersion ?? assembly?.GetName().Version?.ToString() ?? "Unknown";
                SoftwareVersionText.Text = $"軟體版本：{version}";

                // OS description available via RuntimeInformation
                string osVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                OSVersionText.Text = $"作業系統：{osVersion}";
            }
            catch
            {
                SoftwareVersionText.Text = "軟體版本：Unknown";
                OSVersionText.Text = "作業系統：Unknown";
            }
        }

        private void BtnLoadLog_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (dpLogDate.SelectedDate.HasValue)
            {
                LoadLogForDate(dpLogDate.SelectedDate.Value);
            }
        }

        private void BtnOpenLogFolder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (!dpLogDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("請先選擇日期", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var date = dpLogDate.SelectedDate.Value.Date;
                string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
                string dir = Path.Combine(baseDir, "OperateLog", date.ToString("yyyyMM"));
                string filePath = Path.Combine(dir, $"OperateLog-{date:yyyyMMdd}.csv");
                string altDir = Path.Combine(baseDir, "TestLog", date.ToString("yyyyMM"));
                string altFile = Path.Combine(altDir, $"OperateLog-{date:yyyyMMdd}.csv");

                string usedFile = null;
                if (File.Exists(filePath)) usedFile = filePath;
                else if (File.Exists(altFile)) usedFile = altFile;

                if (usedFile == null)
                {
                    MessageBox.Show($"找不到 {date:yyyy-MM-dd} 的日誌檔案。", "查無檔案", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Open Explorer and select the file
                Process.Start("explorer.exe", $"/select,\"{usedFile}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"無法開啟資料夾: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDownload_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_lastLoadedLogFile) || !File.Exists(_lastLoadedLogFile))
                {
                    MessageBox.Show("沒有可下載的日誌檔案，請先讀取。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    btnDownload.IsEnabled = false;
                    return;
                }

                var dlg = new SaveFileDialog()
                {
                    Title = "另存日誌檔案",
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    FileName = Path.GetFileName(_lastLoadedLogFile),
                    OverwritePrompt = true
                };

                bool? res = dlg.ShowDialog();
                if (res == true)
                {
                    try
                    {
                        File.Copy(_lastLoadedLogFile, dlg.FileName, true);
                        MessageBox.Show($"檔案已下載到: {dlg.FileName}", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"下載失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"下載處理失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadLogForDate(DateTime date)
        {
            try
            {
                // First check whether a log file exists for this date so we can give feedback
                string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
                string dir = Path.Combine(baseDir, "OperateLog", date.ToString("yyyyMM"));
                string filePath = Path.Combine(dir, $"OperateLog-{date:yyyyMMdd}.csv");
                string altDir = Path.Combine(baseDir, "TestLog", date.ToString("yyyyMM"));
                string altFile = Path.Combine(altDir, $"OperateLog-{date:yyyyMMdd}.csv");

                string usedFile = null;
                if (File.Exists(filePath)) usedFile = filePath;
                else if (File.Exists(altFile)) usedFile = altFile;

                _lastLoadedLogFile = usedFile;
                btnDownload.IsEnabled = !string.IsNullOrEmpty(_lastLoadedLogFile);

                if (usedFile == null)
                {
                    // No file found for this date
                    logGrid.ItemsSource = new List<OperateLogEntry>();
                    LogStatusText.Text = $"找不到 {date:yyyy-MM-dd} 的日誌檔案。";
                    return;
                }

                List<OperateLogEntry> entries = OperateLog.GetEntriesForDate(date);
                if (entries == null || entries.Count ==0)
                {
                    logGrid.ItemsSource = new List<OperateLogEntry>();
                    LogStatusText.Text = $"已讀取檔案 {usedFile}，但 {date:yyyy-MM-dd} 無日誌資料。";
                    return;
                }

                logGrid.ItemsSource = entries.OrderByDescending(x => x.Timestamp).ToList();
                LogStatusText.Text = $"已載入 {entries.Count} 筆，來源: {usedFile}";
            }
            catch (Exception ex)
            {
                logGrid.ItemsSource = new List<OperateLogEntry>();
                LogStatusText.Text = $"讀取日誌失敗: {ex.Message}";
            }
        }

        private void DpLogDate_CalendarOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                // OperateLog base directory
                string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
                string operateBase = Path.Combine(baseDir, "OperateLog");
                if (!Directory.Exists(operateBase)) return;

                // Collect available dates from files under OperateLog/yyyyMM/OperateLog-yyyyMMdd.csv
                var dateSet = Directory.EnumerateFiles(operateBase, "OperateLog-*.csv", SearchOption.AllDirectories)
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .Where(n => n.StartsWith("OperateLog-") && DateTime.TryParseExact(n.Substring("OperateLog-".Length), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    .Select(n => DateTime.ParseExact(n.Substring("OperateLog-".Length), "yyyyMMdd", CultureInfo.InvariantCulture))
                    .ToHashSet();

                // Also include TestLog folder if exists
                string testBase = Path.Combine(baseDir, "TestLog");
                if (Directory.Exists(testBase))
                {
                    var testDates = Directory.EnumerateFiles(testBase, "OperateLog-*.csv", SearchOption.AllDirectories)
                        .Select(f => Path.GetFileNameWithoutExtension(f))
                        .Where(n => n.StartsWith("OperateLog-") && DateTime.TryParseExact(n.Substring("OperateLog-".Length), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                        .Select(n => DateTime.ParseExact(n.Substring("OperateLog-".Length), "yyyyMMdd", CultureInfo.InvariantCulture));
                    foreach (var d in testDates) dateSet.Add(d);
                }

                // Find Calendar inside DatePicker template
                if (dpLogDate.Template.FindName("PART_Popup", dpLogDate) is System.Windows.Controls.Primitives.Popup popup && popup.Child is System.Windows.Controls.Calendar cal)
                {
                    HighlightCalendarDays(cal, dateSet);
                }
                else
                {
                    var calFound = FindVisualChild<System.Windows.Controls.Calendar>(dpLogDate);
                    if (calFound != null)
                        HighlightCalendarDays(calFound, dateSet);
                }
            }
            catch
            {
                // ignore
            }
        }

        private void HighlightCalendarDays(System.Windows.Controls.Calendar calendar, System.Collections.Generic.HashSet<DateTime> dates)
        {
            if (calendar == null || dates == null || dates.Count ==0)
                return;

            calendar.Loaded += (s, e) => ApplyHighlight();
            ApplyHighlight();

            void ApplyHighlight()
            {
                var dayButtons = GetCalendarDayButtons(calendar);
                foreach (var btn in dayButtons)
                {
                    if (btn.DataContext is DateTime dt)
                    {
                        var key = dt.Date;
                        if (dates.Contains(key))
                        {
                            btn.Background = new SolidColorBrush(Colors.LightGreen);
                            btn.ToolTip = "有日誌紀錄";
                        }
                        else
                        {
                            btn.ClearValue(Button.BackgroundProperty);
                            btn.ToolTip = null;
                        }
                    }
                }
            }
        }

        private static System.Collections.Generic.IEnumerable<CalendarDayButton> GetCalendarDayButtons(DependencyObject parent)
        {
            var list = new System.Collections.Generic.List<CalendarDayButton>();
            GetChildren(parent, list);
            return list;

            void GetChildren(DependencyObject d, System.Collections.Generic.List<CalendarDayButton> acc)
            {
                if (d == null)
                    return;
                var count = VisualTreeHelper.GetChildrenCount(d);
                for (int i =0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(d, i);
                    if (child is CalendarDayButton cdb)
                    {
                        acc.Add(cdb);
                    }
                    GetChildren(child, acc);
                }
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;
            for (int i =0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    return t;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
