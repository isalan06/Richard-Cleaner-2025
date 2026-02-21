using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using System.Reflection;
using System.Windows.Media;

namespace CleanerControlApp.Vision.Developer
{
    public partial class PlcTest_ParameterView : UserControl
    {
        private DateTime? _writeStartUtc;

        public PlcTest_ParameterView()
        {
            InitializeComponent();

            // subscribe to parameter read completed event if service is available
            var svc = App.AppHost?.Services.GetService(typeof(IPLCService)) as IPLCService;
            if (svc != null)
            {
                // use weak event pattern avoid memory leak? simple subscription
                svc.ParametersReadCompleted += Svc_ParametersReadCompleted;
                svc.ParametersWriteCompleted += Svc_ParametersWriteCompleted;
            }
        }

        private void Svc_ParametersReadCompleted(object? sender, System.EventArgs e)
        {
            // UI update must run on UI thread
            Dispatcher.Invoke(() =>
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;

                // refresh displayed values from operator
                var op = App.AppHost?.Services.GetService(typeof(IPLCOperator)) as IPLCOperator;
                if (op != null)
                {
                    // Axis1
                    Axis1_JogSpeedH_Display.Text = op.Param_Read_Axis1JogSpeedH.ToString();
                    Axis1_JogSpeedM_Display.Text = op.Param_Read_Axis1JogSpeedM.ToString();
                    Axis1_JogSpeedL_Display.Text = op.Param_Read_Axis1JogSpeedL.ToString();
                    Axis1_HomeSpeedH_Display.Text = op.Param_Read_Axis1HomeSpeedH.ToString();
                    Axis1_HomeSpeedM_Display.Text = op.Param_Read_Axis1HomeSpeedM.ToString();
                    Axis1_HomeSpeedL_Display.Text = op.Param_Read_Axis1HomeSpeedL.ToString();
                    Axis1_HomeTimeout_Display.Text = op.Param_Read_Axis1HomeTimeoutValue_ms.ToString();
                    Axis1_CommandTimeout_Display.Text = op.Param_Read_Axis1CommandTimeoutValue_ms.ToString();

                    // populate input boxes with read values
                    Axis1_JogSpeedH_Input.Text = op.Param_Read_Axis1JogSpeedH.ToString();
                    Axis1_JogSpeedM_Input.Text = op.Param_Read_Axis1JogSpeedM.ToString();
                    Axis1_JogSpeedL_Input.Text = op.Param_Read_Axis1JogSpeedL.ToString();
                    Axis1_HomeSpeedH_Input.Text = op.Param_Read_Axis1HomeSpeedH.ToString();
                    Axis1_HomeSpeedM_Input.Text = op.Param_Read_Axis1HomeSpeedM.ToString();
                    Axis1_HomeSpeedL_Input.Text = op.Param_Read_Axis1HomeSpeedL.ToString();
                    Axis1_HomeTimeout_Input.Text = op.Param_Read_Axis1HomeTimeoutValue_ms.ToString();
                    Axis1_CommandTimeout_Input.Text = op.Param_Read_Axis1CommandTimeoutValue_ms.ToString();

                    // Axis2
                    Axis2_JogSpeedH_Display.Text = op.Param_Read_Axis2JogSpeedH.ToString();
                    Axis2_JogSpeedM_Display.Text = op.Param_Read_Axis2JogSpeedM.ToString();
                    Axis2_JogSpeedL_Display.Text = op.Param_Read_Axis2JogSpeedL.ToString();
                    Axis2_HomeSpeedH_Display.Text = op.Param_Read_Axis2HomeSpeedH.ToString();
                    Axis2_HomeSpeedM_Display.Text = op.Param_Read_Axis2HomeSpeedM.ToString();
                    Axis2_HomeSpeedL_Display.Text = op.Param_Read_Axis2HomeSpeedL.ToString();
                    Axis2_HomeTimeout_Display.Text = op.Param_Read_Axis2HomeTimeoutValue_ms.ToString();
                    Axis2_CommandTimeout_Display.Text = op.Param_Read_Axis2CommandTimeoutValue_ms.ToString();

                    Axis2_JogSpeedH_Input.Text = op.Param_Read_Axis2JogSpeedH.ToString();
                    Axis2_JogSpeedM_Input.Text = op.Param_Read_Axis2JogSpeedM.ToString();
                    Axis2_JogSpeedL_Input.Text = op.Param_Read_Axis2JogSpeedL.ToString();
                    Axis2_HomeSpeedH_Input.Text = op.Param_Read_Axis2HomeSpeedH.ToString();
                    Axis2_HomeSpeedM_Input.Text = op.Param_Read_Axis2HomeSpeedM.ToString();
                    Axis2_HomeSpeedL_Input.Text = op.Param_Read_Axis2HomeSpeedL.ToString();
                    Axis2_HomeTimeout_Input.Text = op.Param_Read_Axis2HomeTimeoutValue_ms.ToString();
                    Axis2_CommandTimeout_Input.Text = op.Param_Read_Axis2CommandTimeoutValue_ms.ToString();

                    // Axis3
                    Axis3_JogSpeedH_Display.Text = op.Param_Read_Axis3JogSpeedH.ToString();
                    Axis3_JogSpeedM_Display.Text = op.Param_Read_Axis3JogSpeedM.ToString();
                    Axis3_JogSpeedL_Display.Text = op.Param_Read_Axis3JogSpeedL.ToString();
                    Axis3_HomeSpeedH_Display.Text = op.Param_Read_Axis3HomeSpeedH.ToString();
                    Axis3_HomeSpeedM_Display.Text = op.Param_Read_Axis3HomeSpeedM.ToString();
                    Axis3_HomeSpeedL_Display.Text = op.Param_Read_Axis3HomeSpeedL.ToString();
                    Axis3_HomeTimeout_Display.Text = op.Param_Read_Axis3HomeTimeoutValue_ms.ToString();
                    Axis3_CommandTimeout_Display.Text = op.Param_Read_Axis3CommandTimeoutValue_ms.ToString();

                    Axis3_JogSpeedH_Input.Text = op.Param_Read_Axis3JogSpeedH.ToString();
                    Axis3_JogSpeedM_Input.Text = op.Param_Read_Axis3JogSpeedM.ToString();
                    Axis3_JogSpeedL_Input.Text = op.Param_Read_Axis3JogSpeedL.ToString();
                    Axis3_HomeSpeedH_Input.Text = op.Param_Read_Axis3HomeSpeedH.ToString();
                    Axis3_HomeSpeedM_Input.Text = op.Param_Read_Axis3HomeSpeedM.ToString();
                    Axis3_HomeSpeedL_Input.Text = op.Param_Read_Axis3HomeSpeedL.ToString();
                    Axis3_HomeTimeout_Input.Text = op.Param_Read_Axis3HomeTimeoutValue_ms.ToString();
                    Axis3_CommandTimeout_Input.Text = op.Param_Read_Axis3CommandTimeoutValue_ms.ToString();

                    // Axis4
                    Axis4_JogSpeedH_Display.Text = op.Param_Read_Axis4JogSpeedH.ToString();
                    Axis4_JogSpeedM_Display.Text = op.Param_Read_Axis4JogSpeedM.ToString();
                    Axis4_JogSpeedL_Display.Text = op.Param_Read_Axis4JogSpeedL.ToString();
                    Axis4_HomeSpeedH_Display.Text = op.Param_Read_Axis4HomeSpeedH.ToString();
                    Axis4_HomeSpeedM_Display.Text = op.Param_Read_Axis4HomeSpeedM.ToString();
                    Axis4_HomeSpeedL_Display.Text = op.Param_Read_Axis4HomeSpeedL.ToString();
                    Axis4_HomeTimeout_Display.Text = op.Param_Read_Axis4HomeTimeoutValue_ms.ToString();
                    Axis4_CommandTimeout_Display.Text = op.Param_Read_Axis4CommandTimeoutValue_ms.ToString();

                    Axis4_JogSpeedH_Input.Text = op.Param_Read_Axis4JogSpeedH.ToString();
                    Axis4_JogSpeedM_Input.Text = op.Param_Read_Axis4JogSpeedM.ToString();
                    Axis4_JogSpeedL_Input.Text = op.Param_Read_Axis4JogSpeedL.ToString();
                    Axis4_HomeSpeedH_Input.Text = op.Param_Read_Axis4HomeSpeedH.ToString();
                    Axis4_HomeSpeedM_Input.Text = op.Param_Read_Axis4HomeSpeedM.ToString();
                    Axis4_HomeSpeedL_Input.Text = op.Param_Read_Axis4HomeSpeedL.ToString();
                    Axis4_HomeTimeout_Input.Text = op.Param_Read_Axis4HomeTimeoutValue_ms.ToString();
                    Axis4_CommandTimeout_Input.Text = op.Param_Read_Axis4CommandTimeoutValue_ms.ToString();
                }

            });
        }

        private async void Svc_ParametersWriteCompleted(object? sender, System.EventArgs e)
        {
            // ensure at least2 seconds visible
            TimeSpan min = TimeSpan.FromSeconds(2);
            DateTime now = DateTime.UtcNow;
            DateTime start = _writeStartUtc ?? now;
            var elapsed = now - start;
            var remaining = min - elapsed;
            if (remaining > TimeSpan.Zero)
            {
                await System.Threading.Tasks.Task.Delay(remaining);
            }

            Dispatcher.Invoke(() =>
            {
                // hide overlay when write completes
                LoadingOverlay.Visibility = Visibility.Collapsed;
            });
        }

        // Allow only digits in the TextBox
        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        // Handle paste operations
        private void NumericOnly_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                var pasteText = e.DataObject.GetData(DataFormats.Text) as string;
                if (pasteText != null && !IsTextNumeric(pasteText))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private bool IsTextNumeric(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            return Regex.IsMatch(text, "^[0-9]+$");
        }

        private void BtnReadParams_Click(object sender, RoutedEventArgs e)
        {
            var svc = App.AppHost?.Services.GetService(typeof(IPLCService)) as IPLCService;
            if (svc != null)
            {
                // show loading overlay
                LoadingOverlay.Visibility = Visibility.Visible;
                svc.ReadParameter();
            }
            else
            {
                // still attempt to read from operator if available
                var op = App.AppHost?.Services.GetService(typeof(IPLCOperator)) as IPLCOperator;
                if (op != null)
                {
                    Axis1_JogSpeedH_Display.Text = op.Param_Read_Axis1JogSpeedH.ToString();
                    // ... other values already populated in earlier code but keep minimal here
                }
            }
        }

        private void BtnWriteParams_Click(object sender, RoutedEventArgs e)
        {
            // Before writing, ensure all input boxes values are pushed into operator/service properties
            ApplyAllInputsToOperator();

            var svc = App.AppHost?.Services.GetService(typeof(IPLCService)) as IPLCService;
            if (svc != null)
            {
                var loadingTextBlock = this.FindName("LoadingText") as TextBlock;
                if (loadingTextBlock != null)
                    loadingTextBlock.Text = "寫入中...";
                LoadingOverlay.Visibility = Visibility.Visible;
                _writeStartUtc = DateTime.UtcNow;
                svc.WriteParameter();
            }
            else
            {
                // fallback hide quickly if no service
                LoadingOverlay.Visibility = Visibility.Visible;
                var loadingTextBlock = this.FindName("LoadingText") as TextBlock;
                if (loadingTextBlock != null)
                    loadingTextBlock.Text = "寫入中...";
                // hide after ~2 seconds
                var _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(2000);
                    Dispatcher.Invoke(() => LoadingOverlay.Visibility = Visibility.Collapsed);
                });
            }
        }

        private async void BtnSet_Click(object sender, RoutedEventArgs e)
        {
            // keep existing click behavior: set true momentarily
            var op = App.AppHost?.Services.GetService(typeof(IPLCOperator)) as IPLCOperator;
            if (op != null)
            {
                var prop = op.GetType().GetProperty("Command_WriteParameter");
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        prop.SetValue(op, true);
                        await System.Threading.Tasks.Task.Delay(200);
                        prop.SetValue(op, false);
                    }
                    catch
                    {
                        // ignore any reflection errors
                    }
                }
                else
                {
                    var svc = op as IPLCService;
                    if (svc != null)
                    {
                        try
                        {
                            if (svc.Command != null && svc.Command.Length >0)
                            {
                                svc.Command[0].Bit2 = true;
                                await System.Threading.Tasks.Task.Delay(200);
                                svc.Command[0].Bit2 = false;
                            }
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                }
            }
        }

        // New: press handlers set the command bit; release handlers clear it
        private void SetCommandWriteParameter(bool value)
        {
            var op = App.AppHost?.Services.GetService(typeof(IPLCOperator)) as IPLCOperator;
            if (op != null)
            {
                var prop = op.GetType().GetProperty("Command_WriteParameter");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(op, value);
                    return;
                }
            }

            // fallback to IPLCService.Command array
            var svc = App.AppHost?.Services.GetService(typeof(IPLCService)) as IPLCService;
            if (svc != null && svc.Command != null && svc.Command.Length >0)
            {
                try
                {
                    svc.Command[0].Bit2 = value;
                }
                catch
                {
                    // ignore any errors writing the bit
                }
            }
        }

        // Optional UI handlers for press/release (wire these up in XAML if needed)
        private void BtnSet_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetCommandWriteParameter(true);
        }

        private void BtnSet_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SetCommandWriteParameter(false);
        }

        // 新增 TouchDown事件處理方法
        private void BtnSet_TouchDown(object sender, TouchEventArgs e)
        {
            SetCommandWriteParameter(true);
            e.Handled = true;
        }

        // 新增 TouchUp事件處理方法
        private void BtnSet_TouchUp(object sender, TouchEventArgs e)
        {
            SetCommandWriteParameter(false);
            e.Handled = true;
        }

        // 新增 LostFocus 事件處理方法
        private void ParamInput_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null)
                return;

            string valueText = textBox.Text ?? string.Empty;
            string paramTag = textBox.Tag as string;
            if (string.IsNullOrEmpty(paramTag))
                return;

            if (!int.TryParse(valueText, out int value))
                return; // invalid number, ignore or add validation feedback

            // Try to set on IPLCOperator first
            var op = App.AppHost?.Services.GetService(typeof(IPLCOperator)) as IPLCOperator;
            if (op != null)
            {
                var prop = op.GetType().GetProperty(paramTag);
                if (prop == null)
                {
                    // try interface PropertyInfo (handles explicit interface impl)
                    prop = typeof(IPLCOperator).GetProperty(paramTag);
                }
                if (prop != null && prop.CanWrite && prop.PropertyType == typeof(int))
                {
                    try
                    {
                        prop.SetValue(op, value);
                        return;
                    }
                    catch
                    {
                        // ignore reflection errors
                    }
                }
            }

            // fallback to IPLCService if operator not available
            var svc = App.AppHost?.Services.GetService(typeof(IPLCService)) as IPLCService;
            if (svc != null)
            {
                var prop = svc.GetType().GetProperty(paramTag);
                if (prop == null)
                {
                    prop = typeof(IPLCService).GetProperty(paramTag);
                }
                if (prop != null && prop.CanWrite && prop.PropertyType == typeof(int))
                {
                    try
                    {
                        prop.SetValue(svc, value);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        // Traverse visual tree and apply all TextBox inputs with Tag starting with "Param_Write_" to operator/service
        private void ApplyAllInputsToOperator()
        {
            var op = App.AppHost?.Services.GetService(typeof(IPLCOperator)) as IPLCOperator;
            var svc = App.AppHost?.Services.GetService(typeof(IPLCService)) as IPLCService;

            void TrySet(string tag, int val)
            {
                if (string.IsNullOrEmpty(tag))
                    return;

                if (op != null)
                {
                    var p = op.GetType().GetProperty(tag);
                    if (p == null)
                    {
                        p = typeof(IPLCOperator).GetProperty(tag);
                    }
                    if (p != null && p.CanWrite && p.PropertyType == typeof(int))
                    {
                        try { p.SetValue(op, val); return; } catch { }
                    }
                }

                if (svc != null)
                {
                    var p = svc.GetType().GetProperty(tag);
                    if (p == null)
                    {
                        p = typeof(IPLCService).GetProperty(tag);
                    }
                    if (p != null && p.CanWrite && p.PropertyType == typeof(int))
                    {
                        try { p.SetValue(svc, val); } catch { }
                    }
                }
            }

            var stack = new System.Collections.Generic.Stack<DependencyObject>();
            stack.Push(this);
            while (stack.Count >0)
            {
                var cur = stack.Pop();
                int count = VisualTreeHelper.GetChildrenCount(cur);
                for (int i =0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(cur, i);
                    if (child is TextBox tb)
                    {
                        var tag = tb.Tag as string;
                        if (!string.IsNullOrEmpty(tag) && tag.StartsWith("Param_Write_") && int.TryParse(tb.Text, out int v))
                        {
                            TrySet(tag, v);
                        }
                    }
                    stack.Push(child);
                }
            }
        }

    }
}
