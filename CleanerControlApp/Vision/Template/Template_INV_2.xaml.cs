using CleanerControlApp.Hardwares;
using CleanerControlApp.Hardwares.HeatingTank.Interfaces;
using CleanerControlApp.Hardwares.Sink.Interfaces;
using Microsoft.Extensions.DependencyInjection;
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

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_INV_2.xaml 的互動邏輯
    /// </summary>
    public partial class Template_INV_2 : UserControl
    {
        private readonly DispatcherTimer _timer;
        private IHeatingTank? _heatingTank;

        // Dependency properties so XAML ElementName bindings and DataTriggers update correctly
        public static readonly DependencyProperty HighINVProperty = DependencyProperty.Register(
            nameof(HighINV), typeof(bool), typeof(Template_INV_2), new PropertyMetadata(false));

        public static readonly DependencyProperty LowINVProperty = DependencyProperty.Register(
            nameof(LowINV), typeof(bool), typeof(Template_INV_2), new PropertyMetadata(false));

        public static readonly DependencyProperty ZeroINVProperty = DependencyProperty.Register(
            nameof(ZeroINV), typeof(bool), typeof(Template_INV_2), new PropertyMetadata(false));

        // New dependency property to represent INV On/Off status mapped to IHeatingTank.FrequencyOn
        public static readonly DependencyProperty INVOnProperty = DependencyProperty.Register(
            nameof(INVOn), typeof(bool), typeof(Template_INV_2), new PropertyMetadata(false));

        public bool HighINV
        {
            get => (bool)GetValue(HighINVProperty);
            set => SetValue(HighINVProperty, value);
        }

        public bool LowINV
        {
            get => (bool)GetValue(LowINVProperty);
            set => SetValue(LowINVProperty, value);
        }

        public bool ZeroINV
        {
            get => (bool)GetValue(ZeroINVProperty);
            set => SetValue(ZeroINVProperty, value);
        }

        public bool INVOn
        {
            get => (bool)GetValue(INVOnProperty);
            set => SetValue(INVOnProperty, value);
        }

        public Template_INV_2()
        {
            InitializeComponent();

            try
            {
                // try to resolve once at construction (may be null in designer or early startup)
                _heatingTank = App.AppHost?.Services.GetService<IHeatingTank>();
            }
            catch
            {
                _heatingTank = null;
            }

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += Timer_Tick;

            Loaded += Template_INV_2_Loaded;
            Unloaded += (s, e) => _timer.Stop();

            UpdateFields();
        }

        private void Template_INV_2_Loaded(object? sender, RoutedEventArgs e)
        {
            _timer.Start();

            // Ensure visual elements are hit-testable; do not attach click handlers here because XAML already binds Click events
            try
            {
                if (MainCanvas != null)
                {
                    MainCanvas.IsHitTestVisible = true;

                    // make any direct Button children hittable
                    var buttons = MainCanvas.Children.OfType<Button>().ToList();
                    if (buttons.Count ==0)
                    {
                        var nested = FindVisualChildren<Button>(MainCanvas).ToList();
                        buttons = nested;
                    }

                    foreach (var btn in buttons)
                    {
                        btn.IsHitTestVisible = true;
                        btn.IsEnabled = true;
                        try { Panel.SetZIndex(btn,1000); } catch { }
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i =0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (var childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateFields();
        }

        private void UpdateFields()
        {
            try
            {
                // attempt to re-resolve service if it was null earlier
                if (_heatingTank == null)
                {
                    try { _heatingTank = App.AppHost?.Services.GetService<IHeatingTank>(); } catch { _heatingTank = null; }
                }

                if (_heatingTank != null)
                {
                    txtCmdFreq.Text = _heatingTank.InvCommandFrequency.ToString("0.00");
                    txtActFreq.Text = _heatingTank.InvActualFrequency.ToString("0.00");
                    txtErrCode.Text = _heatingTank.InvErrorCode.ToString();
                    txtWarnCode.Text = _heatingTank.InvWarningCode.ToString();

                    // Map heating tank frequency flags to UI properties
                    HighINV = _heatingTank.IsHighFrequency;
                    LowINV = _heatingTank.IsLowFrequency;
                    ZeroINV = _heatingTank.IsZeroFrequency;

                    // Map FrequencyOn to INVOn for the On indicator
                    try { INVOn = _heatingTank.FrequencyOn; } catch { INVOn = false; }
                }
                else
                {
                    txtCmdFreq.Text = "0.00";
                    txtActFreq.Text = "0.00";
                    txtErrCode.Text = "0";
                    txtWarnCode.Text = "0";

                    HighINV = false;
                    LowINV = false;
                    ZeroINV = false;
                    INVOn = false;
                }
            }
            catch
            {
                // ignore UI update errors
            }
        }

        // Helper to get heating tank at time of action and show friendly message if not available
        private IHeatingTank? GetHeatingTankOrShowError()
        {
            if (_heatingTank == null)
            {
                try { _heatingTank = App.AppHost?.Services.GetService<IHeatingTank>(); } catch { _heatingTank = null; }
            }

            if (_heatingTank == null)
            {
                try { CleanerControlApp.Vision.Shared.StatusPopup.Show("HeatingTank service not available.", Window.GetWindow(this),3); } catch { }
            }

            return _heatingTank;
        }

        // INV button handlers -> call ManualFrequencyOP with specified codes
        private void INV_High_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ht = GetHeatingTankOrShowError();
                if (ht != null)
                {
                    ht.ManualFrequencyOP(2);
                    var status = ht.MessageForOperation;
                    if (!string.IsNullOrEmpty(status))
                    {
                        ShowStatusPopup(status);
                    }
                }
            }
            catch
            {
                // ignore
            }
            UpdateFields();
        }

        private void INV_Low_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ht = GetHeatingTankOrShowError();
                if (ht != null)
                {
                    ht.ManualFrequencyOP(1);
                    var status = ht.MessageForOperation;
                    if (!string.IsNullOrEmpty(status))
                    {
                        ShowStatusPopup(status);
                    }
                }
            }
            catch
            {
                // ignore
            }
            UpdateFields();
        }

        private void INV_Zero_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ht = GetHeatingTankOrShowError();
                if (ht != null)
                {
                    ht.ManualFrequencyOP(0);
                }
            }
            catch
            {
                // ignore
            }
            UpdateFields();
        }

        private void ShowStatusPopup(string status)
        {
            try
            {
                CleanerControlApp.Vision.Shared.StatusPopup.Show(status, Window.GetWindow(this),10);
            }
            catch { }
        }
    }
}
