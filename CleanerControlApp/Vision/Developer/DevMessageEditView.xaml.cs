using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualBasic.FileIO;
using CleanerControlApp.Utilities.Alarm;
using System.Windows.Documents;

namespace CleanerControlApp.Vision.Developer
{
    public partial class DevMessageEditView : UserControl
    {
        private List<string> _allAlarmCodes = new List<string>();

        public DevMessageEditView()
        {
            InitializeComponent();
            Loaded += DevMessageEditView_Loaded;
        }

        private void DevMessageEditView_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_allAlarmCodes.Count == 0)
                {
                    var csvPath = FindAlarmCsv();
                    if (!string.IsNullOrEmpty(csvPath) && File.Exists(csvPath))
                    {
                        using (var parser = new TextFieldParser(csvPath))
                        {
                            parser.TextFieldType = FieldType.Delimited;
                            parser.SetDelimiters(",");
                            parser.HasFieldsEnclosedInQuotes = true;

                            // Read header
                            string[] headers = null;
                            if (!parser.EndOfData)
                            {
                                headers = parser.ReadFields();
                            }

                            int codeIndex = 0; // default first column
                            if (headers != null)
                            {
                                for (int i = 0; i < headers.Length; i++)
                                {
                                    if (string.Equals(headers[i], "Code", StringComparison.OrdinalIgnoreCase))
                                    {
                                        codeIndex = i;
                                        break;
                                    }
                                }
                            }

                            while (!parser.EndOfData)
                            {
                                string[] fields = parser.ReadFields();
                                if (fields != null && fields.Length > codeIndex)
                                {
                                    var code = fields[codeIndex]?.Trim();
                                    if (!string.IsNullOrEmpty(code) && !_allAlarmCodes.Contains(code))
                                    {
                                        _allAlarmCodes.Add(code);
                                    }
                                }
                            }
                        }
                    }
                }

                // Populate right-side DataGrid with all alarms
                try
                {
                    var list = AlarmList.Alarms.Values.OrderBy(a => a.Code).ToList();
                    dgAlarmList.ItemsSource = list;
                    dgAlarmList.SelectionChanged += DgAlarmList_SelectionChanged;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error populating alarm DataGrid: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading alarm CSV: " + ex.Message);
            }
        }

        private void DgAlarmList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (dgAlarmList.SelectedItem is AlarmInfo info)
            {
                txtAlarmSearch.Text = info.Code;
                UpdateSelectedAlarmInfo(info.Code);
            }
        }

        private void txtAlarmSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = txtAlarmSearch.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                popupSuggestions.IsOpen = false;
                lbAlarmSuggestions.ItemsSource = null;
                return;
            }

            var filtered = _allAlarmCodes
                .Where(c => c.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (filtered.Count == 0)
            {
                popupSuggestions.IsOpen = false;
                lbAlarmSuggestions.ItemsSource = null;
                return;
            }

            lbAlarmSuggestions.ItemsSource = filtered;
            popupSuggestions.IsOpen = true;
            lbAlarmSuggestions.SelectedIndex = 0;

            // if user typed an exact code that exists, update info immediately
            if (filtered.Count == 1 && string.Equals(filtered[0], text, StringComparison.OrdinalIgnoreCase))
            {
                UpdateSelectedAlarmInfo(filtered[0]);
            }
        }

        private void lbAlarmSuggestions_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lbAlarmSuggestions.SelectedItem is string s)
            {
                txtAlarmSearch.Text = s;
                popupSuggestions.IsOpen = false;
                txtAlarmSearch.CaretIndex = txtAlarmSearch.Text.Length;
                UpdateSelectedAlarmInfo(s);
            }
        }

        private void txtAlarmSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (!popupSuggestions.IsOpen) return;

            if (e.Key == Key.Down)
            {
                lbAlarmSuggestions.SelectedIndex = Math.Min(lbAlarmSuggestions.SelectedIndex + 1, lbAlarmSuggestions.Items.Count - 1);
                lbAlarmSuggestions.ScrollIntoView(lbAlarmSuggestions.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                lbAlarmSuggestions.SelectedIndex = Math.Max(lbAlarmSuggestions.SelectedIndex - 1, 0);
                lbAlarmSuggestions.ScrollIntoView(lbAlarmSuggestions.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                if (lbAlarmSuggestions.SelectedItem is string s)
                {
                    txtAlarmSearch.Text = s;
                    txtAlarmSearch.CaretIndex = txtAlarmSearch.Text.Length;
                    UpdateSelectedAlarmInfo(s);
                }
                popupSuggestions.IsOpen = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                popupSuggestions.IsOpen = false;
                e.Handled = true;
            }
        }

        private void UpdateSelectedAlarmInfo(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                ClearDisplay();
                return;
            }

            txtCodeDisplay.Text = $"錯誤碼: {code}";

            if (AlarmList.TryGetAlarm(code, out var info))
            {
                txtTypeDisplay.Text = $"類別: {info.Type}";
                txtModuleDisplay.Text = $"模組: {info.Module}";
                txtDescriptionDisplay.Text = $"描述: {info.Description}";

                // set RichTextBox content
                rtbSolution.Document.Blocks.Clear();
                if (!string.IsNullOrEmpty(info.Solution))
                {
                    var para = new Paragraph(new Run(info.Solution));
                    rtbSolution.Document.Blocks.Add(para);
                }

                // update selection in DataGrid to match
                try
                {
                    dgAlarmList.SelectedItem = AlarmList.Alarms.Values.FirstOrDefault(a => string.Equals(a.Code, code, StringComparison.OrdinalIgnoreCase));
                    dgAlarmList.ScrollIntoView(dgAlarmList.SelectedItem);
                }
                catch { }
            }
            else
            {
                txtTypeDisplay.Text = "類別: ";
                txtModuleDisplay.Text = "模組: ";
                txtDescriptionDisplay.Text = "描述: ";
                rtbSolution.Document.Blocks.Clear();
            }
        }

        private void ClearDisplay()
        {
            txtCodeDisplay.Text = "錯誤碼: ";
            txtTypeDisplay.Text = "類別: ";
            txtModuleDisplay.Text = "模組: ";
            txtDescriptionDisplay.Text = "描述: ";
            rtbSolution.Document.Blocks.Clear();
        }

        private string FindAlarmCsv()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 8 && dir != null; i++)
                {
                    var candidate = Path.Combine(dir.FullName, "AlarmMessage.csv");
                    if (File.Exists(candidate)) return candidate;
                    dir = dir.Parent;
                }

                var fallback = Path.Combine(Directory.GetCurrentDirectory(), "AlarmMessage.csv");
                if (File.Exists(fallback)) return fallback;
            }
            catch { }

            return null;
        }

        private void BtnSaveSolution_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var codeText = txtCodeDisplay.Text?.Replace("錯誤碼:", string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(codeText))
                {
                    MessageBox.Show("請先選擇一個錯誤碼再儲存說明", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // extract plain text from RichTextBox
                string solutionText = string.Empty;
                var range = new TextRange(rtbSolution.Document.ContentStart, rtbSolution.Document.ContentEnd);
                solutionText = range.Text?.Trim() ?? string.Empty;

                bool ok = AlarmList.UpdateSolution(codeText, solutionText);
                if (ok)
                {
                    MessageBox.Show("說明已儲存", "完成", MessageBoxButton.OK, MessageBoxImage.Information);

                    // refresh DataGrid to reflect updated solution
                    try
                    {
                        var list = AlarmList.Alarms.Values.OrderBy(a => a.Code).ToList();
                        dgAlarmList.ItemsSource = list;
                    }
                    catch { }
                }
                else
                {
                    MessageBox.Show("儲存失敗", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}