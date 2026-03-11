using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Collections.Generic;
using CleanerControlApp.Utilities.Alarm;
using Microsoft.Win32;
using System.Windows.Data;

namespace CleanerControlApp.Vision
{
    /// <summary>
    /// AlarmView.xaml 的互動邏輯
    /// </summary>
    public partial class AlarmView : UserControl
    {
        // path of the last loaded history CSV file (used for download)
        private string? _lastLoadedHistoryFile;

        public AlarmView()
        {
            InitializeComponent();
            // ensure download button disabled until a file is loaded
            try { btnDownload.IsEnabled = false; } catch { }

            // Subscribe to alarm changes to refresh UI
            try
            {
                AlarmManager.AlarmsChanged += OnAlarmsChanged;
                this.Unloaded += AlarmView_Unloaded;
            }
            catch { }
        }

        private void AlarmView_Unloaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                AlarmManager.AlarmsChanged -= OnAlarmsChanged;
                this.Unloaded -= AlarmView_Unloaded;
            }
            catch { }
        }

        private void OnAlarmsChanged()
        {
            // Ensure UI thread
            try
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (Resources["AllAlarms"] is ObjectDataProvider odp)
                        {
                            odp.Refresh();
                        }
                        else
                        {
                            // fallback: rebind ItemsSource directly
                            realTimeGrid.ItemsSource = AlarmManager.GetAllEntries();
                        }
                    }
                    catch { }
                });
            }
            catch { }
        }

        private void RealTimeGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (realTimeGrid.SelectedItem is AlarmEntry entry)
            {
                if (AlarmList.TryGetAlarm(entry.Code, out var info))
                {
                    txtSolution.Text = info.Solution;
                    // also populate code query textbox
                    txtCodeQuery.Text = entry.Code;
                    return;
                }

                // If not found, clear or show fallback
                txtSolution.Text = string.Empty;
            }
            else
            {
                txtSolution.Text = string.Empty;
            }
        }

        private void BtnQuery_Click(object sender, RoutedEventArgs e)
        {
            var code = txtCodeQuery.Text?.Trim();
            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("請輸入錯誤代碼", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSolution.Text = string.Empty;
                return;
            }

            if (AlarmList.TryGetAlarm(code, out var info))
            {
                txtSolution.Text = info.Solution;
            }
            else
            {
                txtSolution.Text = string.Empty;
                MessageBox.Show($"找不到錯誤代碼: {code}", "查無錯誤碼", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DpHistoryDate_CalendarOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                // Alarm log base directory
                var baseDir = AlarmManager.LogBaseDirectory;
                if (string.IsNullOrEmpty(baseDir))
                    return;

                if (!Directory.Exists(baseDir))
                    return;

                // Find all yyyyMMdd.csv files in subfolders (folder named yyyyMM) and collect dates that exist
                var dateSet = Directory.EnumerateFiles(baseDir, "*.csv", SearchOption.AllDirectories)
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .Where(n => DateTime.TryParseExact(n, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    .Select(n => DateTime.ParseExact(n, "yyyyMMdd", CultureInfo.InvariantCulture))
                    .ToHashSet();

                // Find Calendar inside DatePicker template
                if (dpHistoryDate.Template.FindName("PART_Popup", dpHistoryDate) is System.Windows.Controls.Primitives.Popup popup && popup.Child is System.Windows.Controls.Calendar cal)
                {
                    HighlightCalendarDays(cal, dateSet);
                }
                else
                {
                    // fallback: try to find Calendar in visual tree
                    var calFound = FindVisualChild<System.Windows.Controls.Calendar>(dpHistoryDate);
                    if (calFound != null)
                        HighlightCalendarDays(calFound, dateSet);
                }
            }
            catch
            {
                // ignore errors
            }
        }

        private void HighlightCalendarDays(System.Windows.Controls.Calendar calendar, System.Collections.Generic.HashSet<DateTime> dates)
        {
            if (calendar == null || dates == null || dates.Count == 0)
                return;

            // Iterate day buttons and set background if date exists
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
                            btn.ToolTip = "有警報紀錄";
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
                for (int i = 0; i < count; i++)
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
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
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

        private void BtnLoadHistory_Click(object sender, RoutedEventArgs e)
        {
            var selected = dpHistoryDate.SelectedDate;
            if (selected == null)
            {
                MessageBox.Show("請選擇日期", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var date = selected.Value.Date;
            var baseDir = AlarmManager.LogBaseDirectory;
            if (string.IsNullOrEmpty(baseDir))
            {
                MessageBox.Show("找不到 AlarmLog目錄設定", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var ym = date.ToString("yyyyMM");
            var day = date.ToString("yyyyMMdd");
            var folder = Path.Combine(baseDir, ym);
            var file = Path.Combine(folder, day + ".csv");

            if (!File.Exists(file))
            {
                MessageBox.Show($"找不到檔案: {file}", "查無檔案", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var lines = File.ReadAllLines(file);
                // Skip header if exists. We expect header contains 'AlarmCode' or similar; assume first line is header if it contains non-numeric letters
                var dataLines = lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
                var list = new List<HistoryRow>();
                foreach (var ln in dataLines)
                {
                    // Simple CSV split handling - supports quoted fields
                    var fields = ParseCsvLine(ln);
                    // Expected columns in the log file (from AlarmManager.WriteLog): AlarmCode,Type,Status,Time,Module,Description,AlarmSN
                    // Map to requested display: AlarmCode, Type, Time, Status->On/Off, Module, Description
                    string code = fields.ElementAtOrDefault(0) ?? string.Empty;
                    string type = fields.ElementAtOrDefault(1) ?? string.Empty;
                    string statusRaw = fields.ElementAtOrDefault(2) ?? string.Empty;
                    string timeStr = fields.ElementAtOrDefault(3) ?? string.Empty;
                    string module = fields.ElementAtOrDefault(4) ?? string.Empty;
                    string description = fields.ElementAtOrDefault(5) ?? string.Empty;

                    DateTime time;
                    if (!DateTime.TryParseExact(timeStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                    {
                        DateTime.TryParse(timeStr, out time);
                    }

                    var status = statusRaw == "Alarm" ? "On" : "Off";

                    list.Add(new HistoryRow
                    {
                        AlarmCode = code,
                        Type = type,
                        Time = time,
                        Status = status,
                        Module = module,
                        Description = description
                    });
                }

                historyGrid.ItemsSource = list.OrderByDescending(x => x.Time).ToList();

                // store path for download and enable button
                _lastLoadedHistoryFile = file;
                try { btnDownload.IsEnabled = true; } catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"讀取檔案失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(line))
                return result.ToArray();

            int i = 0;
            while (i < line.Length)
            {
                if (line[i] == '"')
                {
                    // quoted
                    i++;
                    int start = i;
                    while (i < line.Length)
                    {
                        if (line[i] == '"')
                        {
                            if (i + 1 < line.Length && line[i + 1] == '"')
                            {
                                // escaped quote
                                i += 2;
                                continue;
                            }
                            break;
                        }
                        i++;
                    }
                    var field = line.Substring(start, i - start).Replace("\"\"", "\"");
                    result.Add(field);
                    // skip quote
                    i++;
                    // skip comma
                    if (i < line.Length && line[i] == ',') i++;
                }
                else
                {
                    int start = i;
                    while (i < line.Length && line[i] != ',') i++;
                    result.Add(line.Substring(start, i - start));
                    if (i < line.Length && line[i] == ',') i++;
                }
            }

            return result.ToArray();
        }

        private class HistoryRow
        {
            public string AlarmCode { get; set; }
            public string Type { get; set; }
            public DateTime Time { get; set; }
            public string Status { get; set; }
            public string Module { get; set; }
            public string Description { get; set; }
        }

        private void HistoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (historyGrid.SelectedItem is HistoryRow row)
            {
                if (AlarmList.TryGetAlarm(row.AlarmCode, out var info))
                {
                    txtSolution.Text = info.Solution;
                    txtCodeQuery.Text = row.AlarmCode;
                    return;
                }

                txtSolution.Text = string.Empty;
                txtCodeQuery.Text = row.AlarmCode;
            }
            else
            {
                txtSolution.Text = string.Empty;
            }
        }

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_lastLoadedHistoryFile) || !File.Exists(_lastLoadedHistoryFile))
            {
                MessageBox.Show("沒有可下載的檔案，請先讀取歷史檔案。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                try { btnDownload.IsEnabled = false; } catch { }
                return;
            }

            var dlg = new SaveFileDialog()
            {
                Title = "另存歷史警報檔案",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = Path.GetFileName(_lastLoadedHistoryFile),
                OverwritePrompt = true
            };

            bool? res = dlg.ShowDialog();
            if (res == true)
            {
                try
                {
                    File.Copy(_lastLoadedHistoryFile, dlg.FileName, true);
                    MessageBox.Show($"檔案已下載到: {dlg.FileName}", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"下載失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
