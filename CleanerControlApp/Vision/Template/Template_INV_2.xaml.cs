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
        private readonly IHeatingTank? _heatingTank;

        // Dependency properties so XAML ElementName bindings and DataTriggers update correctly
        public static readonly DependencyProperty HighINVProperty = DependencyProperty.Register(
            nameof(HighINV), typeof(bool), typeof(Template_INV_2), new PropertyMetadata(false));

        public static readonly DependencyProperty LowINVProperty = DependencyProperty.Register(
            nameof(LowINV), typeof(bool), typeof(Template_INV_2), new PropertyMetadata(false));

        public static readonly DependencyProperty ZeroINVProperty = DependencyProperty.Register(
            nameof(ZeroINV), typeof(bool), typeof(Template_INV_2), new PropertyMetadata(false));

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

        public Template_INV_2()
        {
            InitializeComponent();

            try
            {
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

            Loaded += (s, e) => _timer.Start();
            Unloaded += (s, e) => _timer.Stop();

            UpdateFields();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateFields();
        }

        private void UpdateFields()
        {
            try
            {
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
                }
            }
            catch
            {
                // ignore UI update errors
            }
        }

        // INV button handlers -> call ManualFrequencyOP with specified codes
        private void INV_High_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _heatingTank?.ManualFrequencyOP(2);
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
                _heatingTank?.ManualFrequencyOP(1);
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
                _heatingTank?.ManualFrequencyOP(0);
            }
            catch
            {
                // ignore
            }
            UpdateFields();
        }
    }
}
