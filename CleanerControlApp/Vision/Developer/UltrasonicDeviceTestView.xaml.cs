using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Globalization;
using CleanerControlApp.Modules.UltrasonicDevice.Interfaces;

namespace CleanerControlApp.Vision.Developer
{
    public partial class UltrasonicDeviceTestView : UserControl
    {
        private readonly IUltrasonicDevice? _ultrasonicDevice;
        private readonly DispatcherTimer _timer;

        public UltrasonicDeviceTestView()
        {
            InitializeComponent();

            // Resolve IUltrasonicDevice via DI from AppHost
            if (App.AppHost != null)
            {
                var svc = App.AppHost.Services.GetService(typeof(IUltrasonicDevice));
                if (svc is IUltrasonicDevice ud)
                {
                    _ultrasonicDevice = ud;
                }
            }

            // fallback to a dummy implementation if not available
            if (_ultrasonicDevice == null)
            {
                _ultrasonicDevice = new DummyUltrasonicDevice();
            }

            // attach button handlers (XAML defines x:Name for buttons)
            try
            {
                btnUDOpen.Click += BtnUDOpen_Click;
                btnUDClose.Click += BtnUDClose_Click;
                btnUDStart.Click += BtnUDStart_Click;
                btnUDStop.Click += BtnUDStop_Click;
            }
            catch { }

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                bool rtuRunning = false;
                try { rtuRunning = _ultrasonicDevice?.ModbusRTUService?.IsRunning ?? false; } catch { }

                ellipseUDDeviceStatus.Fill = rtuRunning ? Brushes.Green : Brushes.Red;
                txtUDDeviceStatus.Text = rtuRunning ? "連線" : "離線";

                bool sysRunning = false;
                try { sysRunning = _ultrasonicDevice?.IsRunning ?? false; } catch { }
                ellipseUDSystemStatus.Fill = sysRunning ? Brushes.Green : Brushes.Red;
                txtUDSystemStatus.Text = sysRunning ? "執行中" : "未執行";

                // Device properties
                try
                {
                    txtUDEnabled.Text = (_ultrasonicDevice?.UltrasonicEnabled ?? false) ? "True" : "False";
                    txtUDSettingCurrent.Text = _ultrasonicDevice?.SettingCurrent.ToString("F2") ?? "-";
                    txtUDFrequency.Text = _ultrasonicDevice?.Frequency.ToString("F2") ?? "-";
                    txtUDTime.Text = _ultrasonicDevice?.Time.ToString() ?? "-";
                    txtUDPower.Text = _ultrasonicDevice?.Power.ToString() ?? "-";

                    txtUDError.Text = (_ultrasonicDevice?.DeviceError ?? false) ? "True" : "False";
                    txtUDTimeout.Text = (_ultrasonicDevice?.DeviceTimeout ?? false) ? "True" : "False";
                }
                catch { }

                // Diagnostics
                try
                {
                    if (_ultrasonicDevice != null)
                    {
                        txtUDLoopIterationCount.Text = _ultrasonicDevice.LoopIterationCount.ToString();
                        txtUDLastLoopMs.Text = _ultrasonicDevice.LastLoopDurationMilliseconds.ToString("F2");
                        txtUDAvgLoopMs.Text = _ultrasonicDevice.AverageLoopDurationMilliseconds.ToString("F2");
                        txtUDCmdExecCount.Text = _ultrasonicDevice.CommandQueueExecutedCount.ToString();
                    }
                    else
                    {
                        txtUDLoopIterationCount.Text = "-";
                        txtUDLastLoopMs.Text = "-";
                        txtUDAvgLoopMs.Text = "-";
                        txtUDCmdExecCount.Text = "-";
                    }
                }
                catch { }
            }
            catch { }
        }

        private async void BtnUDOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var svc = _ultrasonicDevice?.ModbusRTUService;
                if (svc != null)
                {
                    bool ok = false;
                    await System.Threading.Tasks.Task.Run(() => { try { ok = svc.Open(); } catch { ok = false; } });
                    bool running = svc.IsRunning;
                    ellipseUDDeviceStatus.Fill = running ? Brushes.Green : Brushes.Red;
                    txtUDDeviceStatus.Text = running ? "連線" : "離線";
                }
            }
            catch { }
        }

        private void BtnUDClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var svc = _ultrasonicDevice?.ModbusRTUService;
                if (svc != null)
                {
                    try { svc.Close(); } catch { }
                    bool running = false;
                    try { running = svc.IsRunning; } catch { }
                    ellipseUDDeviceStatus.Fill = running ? Brushes.Green : Brushes.Red;
                    txtUDDeviceStatus.Text = running ? "連線" : "離線";
                }
            }
            catch { }
        }

        private void BtnUDStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ud = _ultrasonicDevice;
                if (ud != null)
                {
                    try { ud.Start(); } catch { }
                    bool sysRunning = false;
                    try { sysRunning = ud.IsRunning; } catch { }
                    ellipseUDSystemStatus.Fill = sysRunning ? Brushes.Green : Brushes.Red;
                    txtUDSystemStatus.Text = sysRunning ? "執行中" : "未執行";
                }
            }
            catch { }
        }

        private void BtnUDStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ud = _ultrasonicDevice;
                if (ud != null)
                {
                    try { ud.Stop(); } catch { }
                    bool sysRunning = false;
                    try { sysRunning = ud.IsRunning; } catch { }
                    ellipseUDSystemStatus.Fill = sysRunning ? Brushes.Green : Brushes.Red;
                    txtUDSystemStatus.Text = sysRunning ? "執行中" : "未執行";
                }
            }
            catch { }
        }

        // Operate buttons handlers
        private void BtnUDOperateOn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ud = _ultrasonicDevice;
                if (ud != null)
                {
                    try { ud.UltrasonicOperate(true); } catch { }
                    // reflect enabled state
                    try { txtUDEnabled.Text = ud.UltrasonicEnabled ? "True" : "False"; } catch { }
                }
            }
            catch { }
        }

        private void BtnUDOperateOff_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ud = _ultrasonicDevice;
                if (ud != null)
                {
                    try { ud.UltrasonicOperate(false); } catch { }
                    // reflect enabled state
                    try { txtUDEnabled.Text = ud.UltrasonicEnabled ? "True" : "False"; } catch { }
                }
            }
            catch { }
        }

        private void BtnUDSetCurrent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var txt = txtUDSetCurrent?.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(txt))
                {
                    MessageBox.Show("請輸入電流值", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!float.TryParse(txt.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                {
                    // Try using current culture as fallback
                    if (!float.TryParse(txt.Trim(), out val))
                    {
                        MessageBox.Show("電流值格式錯誤", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // call device API
                try
                {
                    _ultrasonicDevice?.SetUltrasonicCurrent(val);
                    // update display
                    txtUDSettingCurrent.Text = val.ToString("F2");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"設定失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch { }
        }

        // Dummy implementation to avoid nulls when DI not configured
        private class DummyUltrasonicDevice : IUltrasonicDevice
        {
            public bool UltrasonicEnabled { get; set; } = false;
            public float SettingCurrent { get; set; } = 0f;
            public float Frequency => 40.0f;
            public int Time => 0;
            public int Power => 0;
            public void SetData(ushort[]? data) { }

            public bool IsRunning => false;
            public void Start() { }
            public void Stop() { }

            public bool DeviceConnected => false;
            public bool DeviceError => false;
            public bool DeviceTimeout => false;

            public CleanerControlApp.Modules.Modbus.Interfaces.IModbusRTUService? ModbusRTUService => null;

            public long LoopIterationCount => 0;
            public double LastLoopDurationMilliseconds => 0.0;
            public double AverageLoopDurationMilliseconds => 0.0;
            public long CommandQueueExecutedCount => 0;

            public void UltrasonicOperate(bool enable) { }
            public void SetUltrasonicCurrent(float current) { }
        }
    }
}