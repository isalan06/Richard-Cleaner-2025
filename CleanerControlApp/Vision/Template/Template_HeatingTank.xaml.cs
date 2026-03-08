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
using CleanerControlApp.Hardwares;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media.Animation;
using CleanerControlApp.Hardwares.HeatingTank.Interfaces;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_HeatingTank.xaml 的互動邏輯
    /// </summary>
    public partial class Template_HeatingTank : UserControl
    {
        private readonly DispatcherTimer _timer;
        private readonly IHeatingTank? _heatingTank;
        private Storyboard? _waterInStoryboard;
        private Storyboard? _rightFlowStoryboard;
        private Storyboard? _leftFlowStoryboard;
        private Storyboard? _upFlowStoryboard;
        private Storyboard? _upOutFlowStoryboard;
        private bool _isWaterInStoryboardRunning = false;
        private bool _isRightStoryboardRunning = false;
        private bool _isLeftStoryboardRunning = false;
        private bool _isUpStoryboardRunning = false;
        private bool _isUpOutStoryboardRunning = false;

        // Expose Command_WaterIn as a dependency property so XAML ElementName bindings (DataTrigger) update
        public static readonly DependencyProperty Command_WaterInProperty = DependencyProperty.Register(
            "Command_WaterIn", typeof(bool), typeof(Template_HeatingTank), new PropertyMetadata(false));

        public bool Command_WaterIn
        {
            get => (bool)GetValue(Command_WaterInProperty);
            set => SetValue(Command_WaterInProperty, value);
        }

        // Expose Command_WaterOut as a dependency property so XAML ElementName bindings update
        public static readonly DependencyProperty Command_WaterOutProperty = DependencyProperty.Register(
            "Command_WaterOut", typeof(bool), typeof(Template_HeatingTank), new PropertyMetadata(false));

        public bool Command_WaterOut
        {
            get => (bool)GetValue(Command_WaterOutProperty);
            set => SetValue(Command_WaterOutProperty, value);
        }

        public Template_HeatingTank()
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
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += Timer_Tick;

            Loaded += Template_HeatingTank_Loaded;
            Unloaded += Template_HeatingTank_Unloaded;
        }

        private void Template_HeatingTank_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                // resolve storyboard resources
                try { _waterInStoryboard = (Storyboard?)FindResource("WaterInStoryboard"); } catch { _waterInStoryboard = null; }
                try { _rightFlowStoryboard = (Storyboard?)FindResource("RightFlowStoryboard"); } catch { _rightFlowStoryboard = null; }
                try { _leftFlowStoryboard = (Storyboard?)FindResource("LeftFlowStoryboard"); } catch { _leftFlowStoryboard = null; }
                try { _upFlowStoryboard = (Storyboard?)FindResource("UpFlowStoryboard"); } catch { _upFlowStoryboard = null; }
                try { _upOutFlowStoryboard = (Storyboard?)FindResource("UpOutFlowStoryboard"); } catch { _upOutFlowStoryboard = null; }

                _timer.Start();

                // ensure initial state applied
                bool initialIn = _heatingTank?.Command_WaterIn ?? false;
                bool initialOut = _heatingTank?.Command_WaterOut ?? false;
                bool initialZero = _heatingTank != null ? _heatingTank.IsZeroFrequency : true;

                Command_WaterIn = initialIn;
                Command_WaterOut = initialOut;

                UpdateWaterInAnimation(initialIn);
                // Right flow now controlled by frequency (IsZeroFrequency == false)
                UpdateRightFlowAnimation(!initialZero);
                UpdateLeftFlowAnimation(!initialOut); // animate left flow when water out command is false
                UpdateUpFlowAnimation(!initialZero); // run up flow when IsZeroFrequency == false
                UpdateUpOutFlowAnimation(initialOut); // UpOut runs when water out command is true
            }
            catch { }
        }

        private void Template_HeatingTank_Unloaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                _timer.Stop();
                if (_waterInStoryboard is not null)
                {
                    try { _waterInStoryboard.Stop(this); } catch { }
                    _isWaterInStoryboardRunning = false;
                }
                if (_rightFlowStoryboard is not null)
                {
                    try { _rightFlowStoryboard.Stop(this); } catch { }
                    _isRightStoryboardRunning = false;
                }
                if (_leftFlowStoryboard is not null)
                {
                    try { _leftFlowStoryboard.Stop(this); } catch { }
                    _isLeftStoryboardRunning = false;
                }
                if (_upFlowStoryboard is not null)
                {
                    try { _upFlowStoryboard.Stop(this); } catch { }
                    _isUpStoryboardRunning = false;
                }
                if (_upOutFlowStoryboard is not null)
                {
                    try { _upOutFlowStoryboard.Stop(this); } catch { }
                    _isUpOutStoryboardRunning = false;
                }
            }
            catch { }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                bool waterIn = _heatingTank?.Command_WaterIn ?? false;
                bool waterOut = _heatingTank?.Command_WaterOut ?? false;
                bool isZero = _heatingTank != null ? _heatingTank.IsZeroFrequency : true;

                // keep dependency properties in sync for XAML bindings
                Command_WaterIn = waterIn;
                Command_WaterOut = waterOut;

                UpdateWaterInAnimation(waterIn);
                // Right flow controlled by frequency (IsZeroFrequency == false)
                UpdateRightFlowAnimation(!isZero);
                UpdateLeftFlowAnimation(!waterOut); // animate left flow when water out command is false
                // Up flow runs when frequency is NOT zero
                UpdateUpFlowAnimation(!isZero);
                // UpOut runs when water out command is true
                UpdateUpOutFlowAnimation(waterOut);
            }
            catch
            {
                // ignore
            }
        }

        private void UpdateWaterInAnimation(bool enable)
        {
            if (enable)
            {
                if (!_isWaterInStoryboardRunning && _waterInStoryboard is not null)
                {
                    try
                    {
                        _waterInStoryboard.Begin(this, true);
                        _isWaterInStoryboardRunning = true;
                    }
                    catch { }
                }
            }
            else
            {
                if (_isWaterInStoryboardRunning && _waterInStoryboard is not null)
                {
                    try
                    {
                        _waterInStoryboard.Stop(this);
                        _isWaterInStoryboardRunning = false;
                    }
                    catch { }
                }
            }
        }

        private void UpdateRightFlowAnimation(bool enable)
        {
            if (enable)
            {
                if (!_isRightStoryboardRunning && _rightFlowStoryboard is not null)
                {
                    try { _rightFlowStoryboard.Begin(this, true); _isRightStoryboardRunning = true; } catch { }
                }
            }
            else
            {
                if (_isRightStoryboardRunning && _rightFlowStoryboard is not null)
                {
                    try { _rightFlowStoryboard.Stop(this); _isRightStoryboardRunning = false; } catch { }
                }
            }
        }

        private void UpdateLeftFlowAnimation(bool enable)
        {
            if (enable)
            {
                if (!_isLeftStoryboardRunning && _leftFlowStoryboard is not null)
                {
                    try { _leftFlowStoryboard.Begin(this, true); _isLeftStoryboardRunning = true; } catch { }
                }
            }
            else
            {
                if (_isLeftStoryboardRunning && _leftFlowStoryboard is not null)
                {
                    try { _leftFlowStoryboard.Stop(this); _isLeftStoryboardRunning = false; } catch { }
                }
            }
        }

        private void UpdateUpFlowAnimation(bool enable)
        {
            if (enable)
            {
                if (!_isUpStoryboardRunning && _upFlowStoryboard is not null)
                {
                    try { _upFlowStoryboard.Begin(this, true); _isUpStoryboardRunning = true; } catch { }
                }
            }
            else
            {
                if (_isUpStoryboardRunning && _upFlowStoryboard is not null)
                {
                    try { _upFlowStoryboard.Stop(this); _isUpStoryboardRunning = false; } catch { }
                }
            }
        }

        private void UpdateUpOutFlowAnimation(bool enable)
        {
            if (enable)
            {
                if (!_isUpOutStoryboardRunning && _upOutFlowStoryboard is not null)
                {
                    try { _upOutFlowStoryboard.Begin(this, true); _isUpOutStoryboardRunning = true; } catch { }
                }
            }
            else
            {
                if (_isUpOutStoryboardRunning && _upOutFlowStoryboard is not null)
                {
                    try { _upOutFlowStoryboard.Stop(this); _isUpOutStoryboardRunning = false; } catch { }
                }
            }
        }

        // Button handlers to control water in manually (wired from XAML)
        public void OpenWaterIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _heatingTank?.ManualWaterInOP(true);
            }
            catch { }
        }

        public void CloseWaterIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _heatingTank?.ManualWaterInOP(false);
            }
            catch { }
        }

        // Button handlers to control water out manually
        public void OpenWaterOut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _heatingTank?.ManualWaterOutOP(true);
            }
            catch { }
        }

        public void CloseWaterOut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _heatingTank?.ManualWaterOutOP(false);
            }
            catch { }
        }

        // Reset alarm handler - calls IHeatingTank.AlarmReset()
        private void ResetAlarm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _heatingTank?.AlarmReset();
            }
            catch
            {
                // ignore
            }
        }
    }
}
