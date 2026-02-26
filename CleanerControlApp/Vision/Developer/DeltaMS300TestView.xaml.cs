using System.Windows.Controls;
using CleanerControlApp.Modules.DeltaMS300.Interfaces;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.Globalization;

namespace CleanerControlApp.Vision.Developer
{
    /// <summary>
    /// Interaction logic for DeltaMS300TestView.xaml
    /// </summary>
    public partial class DeltaMS300TestView : UserControl
    {
        private IDeltaMS300[]? _modules;
        private DispatcherTimer? _refreshTimer;

        public DeltaMS300TestView()
        {
            InitializeComponent();
            Loaded += DeltaMS300TestView_Loaded;
            Unloaded += DeltaMS300TestView_Unloaded;
        }

        private void DeltaMS300TestView_Loaded(object? sender, RoutedEventArgs e)
        {
            // Try to resolve IDeltaMS300[] from the host DI container
            try
            {
                _modules = App.AppHost?.Services.GetService(typeof(IDeltaMS300[])) as IDeltaMS300[];
            }
            catch { _modules = null; }

            // Set DataContext for each module border so XAML bindings work
            if (_modules != null)
            {
                if (_modules.Length >0)
                    borderMS0.DataContext = _modules[0];
                if (_modules.Length >1)
                    borderMS1.DataContext = _modules[1];
            }
            else
            {
                borderMS0.DataContext = null;
                borderMS1.DataContext = null;
            }

            // Attach button handlers
            btnMS0Open.Click += BtnMS0Open_Click;
            btnMS0Close.Click += BtnMS0Close_Click;
            btnMS0Start.Click += BtnMS0Start_Click;
            btnMS0Stop.Click += BtnMS0Stop_Click;

            btnMS1Open.Click += BtnMS1Open_Click;
            btnMS1Close.Click += BtnMS1Close_Click;
            btnMS1Start.Click += BtnMS1Start_Click;
            btnMS1Stop.Click += BtnMS1Stop_Click;

            // Start a UI refresh timer
            _refreshTimer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            // Initial refresh
            RefreshTimer_Tick(this, EventArgs.Empty);
        }

        private void DeltaMS300TestView_Unloaded(object? sender, RoutedEventArgs e)
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Tick -= RefreshTimer_Tick;
                _refreshTimer = null;
            }
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (_modules == null || _modules.Length <2)
            {
                // Show unavailable state
                txtMS0DeviceStatus.Text = "N/A";
                txtMS0SystemStatus.Text = "N/A";
                txtMS0LoopIterationCount.Text = "-";
                txtMS0LastLoopMs.Text = "-";
                txtMS0AvgLoopMs.Text = "-";
                txtMS0CmdExecCount.Text = "-";
                txtMS0Error.Text = "-";
                txtMS0Timeout.Text = "-";

                txtMS1DeviceStatus.Text = "N/A";
                txtMS1SystemStatus.Text = "N/A";
                txtMS1LoopIterationCount.Text = "-";
                txtMS1LastLoopMs.Text = "-";
                txtMS1AvgLoopMs.Text = "-";
                txtMS1CmdExecCount.Text = "-";
                txtMS1Error.Text = "-";
                txtMS1Timeout.Text = "-";

                // Clear設備資訊 fields
                txtMS0FreqCommand.Text = "-";
                txtMS0FreqOutput.Text = "-";
                txtMS0FreqSet.Text = "-";

                txtMS1FreqCommand.Text = "-";
                txtMS1FreqOutput.Text = "-";
                txtMS1FreqSet.Text = "-";

                ellipseMS0DeviceStatus.Fill = Brushes.Gray;
                ellipseMS1DeviceStatus.Fill = Brushes.Gray;

                return;
            }

            UpdateModuleUI(0, _modules[0]);
            UpdateModuleUI(1, _modules[1]);
        }

        private void UpdateModuleUI(int idx, IDeltaMS300 module)
        {
            // Choose target controls based on idx
            var deviceEllipse = idx ==0 ? ellipseMS0DeviceStatus : ellipseMS1DeviceStatus;
            var systemEllipse = idx ==0 ? ellipseMS0SystemStatus : ellipseMS1SystemStatus;
            var txtDevice = idx ==0 ? txtMS0DeviceStatus : txtMS1DeviceStatus;
            var txtSystem = idx ==0 ? txtMS0SystemStatus : txtMS1SystemStatus;
            var txtLoopCount = idx ==0 ? txtMS0LoopIterationCount : txtMS1LoopIterationCount;
            var txtLastMs = idx ==0 ? txtMS0LastLoopMs : txtMS1LastLoopMs;
            var txtAvgMs = idx ==0 ? txtMS0AvgLoopMs : txtMS1AvgLoopMs;
            var txtCmdCount = idx ==0 ? txtMS0CmdExecCount : txtMS1CmdExecCount;
            var txtErr = idx ==0 ? txtMS0Error : txtMS1Error;
            var txtTimeout = idx ==0 ? txtMS0Timeout : txtMS1Timeout;

            // Frequency textblocks
            var txtFreqCommand = idx ==0 ? txtMS0FreqCommand : txtMS1FreqCommand;
            var txtFreqOutput = idx ==0 ? txtMS0FreqOutput : txtMS1FreqOutput;
            var txtFreqSet = idx ==0 ? txtMS0FreqSet : txtMS1FreqSet;

            // Determine connection status from ModbusRTUService.IsRunning when available
            var svc = module.ModbusRTUService;
            if (svc != null)
            {
                if (svc.IsRunning)
                {
                    deviceEllipse.Fill = Brushes.Green;
                    txtDevice.Text = "已連線";
                }
                else
                {
                    // If service exists but not running, show disconnected state
                    deviceEllipse.Fill = Brushes.Red;
                    txtDevice.Text = "未連線";
                }
            }
            else
            {
                // Fallback: use module flags for timeout/connected if service not provided
                if (module.DeviceConnected)
                {
                    deviceEllipse.Fill = Brushes.Green;
                    txtDevice.Text = "已連線";
                }
                else if (module.DeviceTimeout)
                {
                    deviceEllipse.Fill = Brushes.Orange;
                    txtDevice.Text = "Timeout";
                }
                else
                {
                    deviceEllipse.Fill = Brushes.Red;
                    txtDevice.Text = "未連線";
                }
            }

            // Update system status text and system status ellipse (綠=執行中, 紅=停止)
            var isRunning = module.IsRunning;
            txtSystem.Text = isRunning ? "執行中" : "停止";
            systemEllipse.Fill = isRunning ? Brushes.Green : Brushes.Red;

            // Diagnostics
            txtLoopCount.Text = module.LoopIterationCount.ToString();
            txtLastMs.Text = module.LastLoopDurationMilliseconds.ToString("F1");
            txtAvgMs.Text = module.AverageLoopDurationMilliseconds.ToString("F1");
            txtCmdCount.Text = module.CommandQueueExecutedCount.ToString();

            txtErr.Text = module.DeviceError ? "True" : "-";
            txtTimeout.Text = module.DeviceTimeout ? "True" : "-";

            // Update frequency display
            try
            {
                txtFreqCommand.Text = module.Frquency_Command.ToString("F2");
                txtFreqOutput.Text = module.Frquency_Output.ToString("F2");
                txtFreqSet.Text = module.Frquency_Set.ToString("F2");
            }
            catch
            {
                txtFreqCommand.Text = "-";
                txtFreqOutput.Text = "-";
                txtFreqSet.Text = "-";
            }
        }

        #region Button handlers (Module0)
        private void BtnMS0Open_Click(object? sender, RoutedEventArgs e)
        {
            if (_modules == null || _modules.Length < 1) return;
            var svc = _modules[0].ModbusRTUService;
            if (svc != null)
            {
                var ok = false;
                try { ok = svc.Open(); } catch { ok = false; }
                txtMS0DeviceStatus.Text = ok ? "已連線" : "開啟失敗";
            }
        }

        private void BtnMS0Close_Click(object? sender, RoutedEventArgs e)
        {
            if (_modules == null || _modules.Length < 1) return;
            var svc = _modules[0].ModbusRTUService;
            try { svc?.Close(); } catch { }
            txtMS0DeviceStatus.Text = "已離線";
        }

        private void BtnMS0Start_Click(object? sender, RoutedEventArgs e)
        {
            if (_modules == null || _modules.Length < 1) return;
            try { _modules[0].Start(); } catch { }
            txtMS0SystemStatus.Text = _modules[0].IsRunning ? "執行中" : "停止";
        }

        private void BtnMS0Stop_Click(object? sender, RoutedEventArgs e)
        {
            if (_modules == null || _modules.Length < 1) return;
            try { _modules[0].Stop(); } catch { }
            txtMS0SystemStatus.Text = _modules[0].IsRunning ? "執行中" : "停止";
        }
        #endregion

        #region Button handlers (Module1)
        private void BtnMS1Open_Click(object? sender, RoutedEventArgs e)
        {
            if (_modules == null || _modules.Length < 2) return;
            var svc = _modules[1].ModbusRTUService;
            if (svc != null)
            {
                var ok = false;
                try { ok = svc.Open(); } catch { ok = false; }
                txtMS1DeviceStatus.Text = ok ? "已連線" : "開啟失敗";
            }
        }

        private void BtnMS1Close_Click(object? sender, RoutedEventArgs e)
        {
            if (_modules == null || _modules.Length < 2) return;
            var svc = _modules[1].ModbusRTUService;
            try { svc?.Close(); } catch { }
            txtMS1DeviceStatus.Text = "已離線";
        }

        private void BtnMS1Start_Click(object? sender, RoutedEventArgs e)
        {
            if (_modules == null || _modules.Length < 2) return;
            try { _modules[1].Start(); } catch { }
            txtMS1SystemStatus.Text = _modules[1].IsRunning ? "執行中" : "停止";
        }

        private void BtnMS1Stop_Click(object? sender, RoutedEventArgs e)
        {
            if (_modules == null || _modules.Length < 2) return;
            try { _modules[1].Stop(); } catch { }
            txtMS1SystemStatus.Text = _modules[1].IsRunning ? "執行中" : "停止";
        }
        #endregion

        #region Frequency set handlers
        private void BtnMS0FreqSetApply_Click(object? sender, RoutedEventArgs e)
        {
            if (_modules == null || _modules.Length <1) return;
            var text = txtMS0FreqSetInput.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            if (!float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
            {
                // try current culture
                if (!float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value))
                {
                    return; // invalid input
                }
            }

            try
            {
                _modules[0].SetFrequency(value);
            }
            catch { }

            txtMS0FreqSet.Text = value.ToString("F2");
        }

        private void BtnMS1FreqSetApply_Click(object? sender, RoutedEventArgs e)
        {
            if (_modules == null || _modules.Length <2) return;
            var text = txtMS1FreqSetInput.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            if (!float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
            {
                // try current culture
                if (!float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value))
                {
                    return; // invalid input
                }
            }

            try
            {
                _modules[1].SetFrequency(value);
            }
            catch { }

            txtMS1FreqSet.Text = value.ToString("F2");
        }
        #endregion

    }
}
