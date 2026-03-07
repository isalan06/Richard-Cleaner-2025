using System;
using System.Windows.Controls;
using System.Windows.Threading;
using CleanerControlApp.Hardwares.Sink.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Tempate_INV_1.xaml 的互動邏輯
    /// </summary>
    public partial class Tempate_INV_1 : UserControl
    {
        private readonly DispatcherTimer _timer;
        private readonly ISink? _sink;

        public Tempate_INV_1()
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
                if (_sink != null)
                {
                    txtCmdFreq.Text = _sink.InvCommandFrequency.ToString("0.00");
                    txtActFreq.Text = _sink.InvActualFrequency.ToString("0.00");
                    txtErrCode.Text = _sink.InvErrorCode.ToString();
                    txtWarnCode.Text = _sink.InvWarningCode.ToString();
                }
                else
                {
                    txtCmdFreq.Text = "0.00";
                    txtActFreq.Text = "0.00";
                    txtErrCode.Text = "0";
                    txtWarnCode.Text = "0";
                }
            }
            catch
            {
                // ignore UI update errors
            }
        }
    }
}
