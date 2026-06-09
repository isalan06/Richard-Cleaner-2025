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
using CleanerControlApp.Hardwares.Shuttle.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Threading;
using CleanerControlApp.Hardwares.Shuttle.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CleanerControlApp.Hardwares;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_Shuttle.xaml 的互動邏輯
    /// </summary>
    public partial class Template_Shuttle : UserControl, INotifyPropertyChanged
    {
        private readonly IShuttle? _shuttle;
        private readonly DispatcherTimer _timer;
        private readonly HardwareManager? _hwManager;

        // teach long-press timer
        private readonly DispatcherTimer _teachHoldTimer;
        private bool _teachTriggered = false;

        // Z teach long-press timer
        private readonly DispatcherTimer _teachHoldTimerZ;
        private bool _teachTriggeredZ = false;

        private bool _inPosX;
        private bool _inPosZ;

        // module status flags
        private bool _autoStatus;
        private bool _pauseStatus;
        private bool _cassetteStatus;
        private bool _idleStatus;
        private bool _initializedStatus;
        private bool _initializingStatus;
        private bool _warningStatus;
        private bool _alarmStatus;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Template_Shuttle()
        {
            InitializeComponent();

            try
            {
                _shuttle = App.AppHost?.Services.GetService<IShuttle>();
            }
            catch
            {
                _shuttle = null;
            }

            try
            {
                _hwManager = App.AppHost?.Services.GetService<HardwareManager>();
            }
            catch
            {
                _hwManager = null;
            }

            // populate cmbXPositionSelect
            try
            {
                if (cmbXPositionSelect != null)
                {
                    cmbXPositionSelect.Items.Clear();
                    for (int i = 0; i < ShuttleXMotorName.Name.Length; i++)
                    {
                        var item = new ComboBoxItem() { Content = ShuttleXMotorName.Name[i], Tag = i }; // position index
                        cmbXPositionSelect.Items.Add(item);
                    }
                    cmbXPositionSelect.SelectedIndex = 0;
                }
            }
            catch
            {
                // ignore
            }

            // populate cmbZPositionSelect
            try
            {
                if (cmbZPositionSelect != null)
                {
                    cmbZPositionSelect.Items.Clear();
                    for (int i = 0; i < ShuttleZMotorName.Name.Length; i++)
                    {
                        var item = new ComboBoxItem() { Content = ShuttleZMotorName.Name[i], Tag = i };
                        cmbZPositionSelect.Items.Add(item);
                    }
                    cmbZPositionSelect.SelectedIndex = 0;
                }
            }
            catch { }

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _timer.Tick += Timer_Tick;

            // teach hold timer (1 second)
            _teachHoldTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(1) };
            _teachHoldTimer.Tick += TeachHoldTimer_Tick;

            // teach hold timer for Z
            _teachHoldTimerZ = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(1) };
            _teachHoldTimerZ.Tick += TeachHoldTimerZ_Tick;

            Loaded += (s, e) =>
            {
                DataContext = this;
                _timer.Start();

                // attach long-press handlers for teach buttons if available
                try
                {
                    if (btnTeachX != null)
                    {
                        btnTeachX.PreviewMouseLeftButtonDown += BtnTeachX_PreviewMouseLeftButtonDown;
                        btnTeachX.PreviewMouseLeftButtonUp += BtnTeachX_PreviewMouseLeftButtonUp;
                        btnTeachX.MouseLeave += BtnTeachX_MouseLeave;
                    }
                    if (btnTeachZ != null)
                    {
                        btnTeachZ.PreviewMouseLeftButtonDown += BtnTeachZ_PreviewMouseLeftButtonDown;
                        btnTeachZ.PreviewMouseLeftButtonUp += BtnTeachZ_PreviewMouseLeftButtonUp;
                        btnTeachZ.MouseLeave += BtnTeachZ_MouseLeave;
                    }
                }
                catch { }
            };

            Unloaded += (s, e) =>
            {
                _timer.Stop();
                _teachHoldTimer.Stop();
                _teachHoldTimerZ.Stop();
            };

            DataContext = this;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (_shuttle != null)
                {
                    if (_shuttle.ShuttleXMotor != null)
                    {
                        int pos = GetSelectedPositionX();
                        InPosX = _shuttle.ShuttleXMotor.GetInPos(pos);
                    }
                    else InPosX = false;

                    if (_shuttle.ShuttleZMotor != null)
                    {
                        int posZ = GetSelectedPositionZ();
                        InPosZ = _shuttle.ShuttleZMotor.GetInPos(posZ);
                    }
                    else InPosZ = false;

                    // update statuses
                    AutoStatus = _shuttle.Auto;
                    PauseStatus = _shuttle.Pausing;
                    CassetteStatus = _shuttle.Cassette;
                    IdleStatus = _shuttle.Idle;
                    InitializedStatus = _shuttle.Initialized;
                    InitializingStatus = _shuttle.Initializing;
                    WarningStatus = _shuttle.HasWarning;
                    AlarmStatus = _shuttle.HasAlarm;
                }
                else
                {
                    InPosX = false;
                    InPosZ = false;

                    AutoStatus = false;
                    PauseStatus = false;
                    CassetteStatus = false;
                    IdleStatus = false;
                    InitializedStatus = false;
                    InitializingStatus = false;
                    WarningStatus = false;
                    AlarmStatus = false;
                }
            }
            catch
            {
                // ignore
            }
        }

        public bool InPosX
        {
            get => _inPosX;
            private set
            {
                if (_inPosX != value)
                {
                    _inPosX = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool InPosZ
        {
            get => _inPosZ;
            private set
            {
                if (_inPosZ != value)
                {
                    _inPosZ = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AutoStatus
        {
            get => _autoStatus;
            private set
            {
                if (_autoStatus != value)
                {
                    _autoStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool PauseStatus
        {
            get => _pauseStatus;
            private set
            {
                if (_pauseStatus != value)
                {
                    _pauseStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CassetteStatus
        {
            get => _cassetteStatus;
            private set
            {
                if (_cassetteStatus != value)
                {
                    _cassetteStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IdleStatus
        {
            get => _idleStatus;
            private set
            {
                if (_idleStatus != value)
                {
                    _idleStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool InitializedStatus
        {
            get => _initializedStatus;
            private set
            {
                if (_initializedStatus != value)
                {
                    _initializedStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        // New property indicating initialization in progress
        public bool InitializingStatus
        {
            get => _initializingStatus;
            private set
            {
                if (_initializingStatus != value)
                {
                    _initializingStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool WarningStatus
        {
            get => _warningStatus;
            private set
            {
                if (_warningStatus != value)
                {
                    _warningStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AlarmStatus
        {
            get => _alarmStatus;
            private set
            {
                if (_alarmStatus != value)
                {
                    _alarmStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        private int GetSelectedPositionX()
        {
            try
            {
                if (cmbXPositionSelect != null && cmbXPositionSelect.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    if (int.TryParse(item.Tag.ToString(), out int tag))
                        return tag;
                }
            }
            catch { }
            return 0;
        }

        private int GetSelectedPositionZ()
        {
            try
            {
                if (cmbZPositionSelect != null && cmbZPositionSelect.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    if (int.TryParse(item.Tag.ToString(), out int tag))
                        return tag;
                }
            }
            catch { }
            return 0;
        }

        private int GetSelectedSpeedX()
        {
            try
            {
                if (cmbXSpeedMode != null && cmbXSpeedMode.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    if (int.TryParse(item.Tag.ToString(), out int tag))
                        return tag;
                }
            }
            catch { }
            return 0;
        }

        private int GetSelectedSpeedZ()
        {
            try
            {
                if (cmbZSpeedMode != null && cmbZSpeedMode.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    if (int.TryParse(item.Tag.ToString(), out int tag))
                        return tag;
                }
            }
            catch { }
            return 0;
        }

        private void ResetAlarm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _shuttle?.AlarmReset();
            }
            catch
            {
                // ignore
            }
        }

        private void OpenClamper_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _shuttle?.ManualClamperOpenOP(true);
                CheckStatus();
            }
            catch
            {
                // ignore
            }
        }

        private void CloseClamper_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _shuttle?.ManualClamperCloseOP(true);
                CheckStatus();
            }
            catch
            {
                // ignore
            }
        }

        private void btnMoveToP1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // If water slot is high (water slot at cassette position or not homed), prompt user before moving
                try
                {
                    var hw = _hwManager ?? App.AppHost?.Services.GetService<HardwareManager>();
                    if (hw != null && hw.WaterSlotPosIsHigh)
                    {
                        bool confirm = CleanerControlApp.Vision.Shared.InfoAskPopup.Ask("水槽已在承載卡匣位置或尚未復歸，請確認是否進行移動?", Window.GetWindow(this));
                        if (!confirm)
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                }
                catch { }

                if (!CheckShuttleXMoveStatus())
                {
                    e.Handled = true;
                    return;
                }

                int pos = GetSelectedPositionX();
                int speed = GetSelectedSpeedX();
                _shuttle?.ShuttleXMotor?.MoveToPosition(pos, speed);
                CheckStatus();
            }
            catch { }
        }

        private void btnTeachP1_Click(object sender, RoutedEventArgs e)
        {
            // No action for short click
        }

        // Long-press handlers for X Teach
        private void TeachHoldTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                _teachHoldTimer.Stop();
                _teachTriggered = true;
                try { if (btnTeachX != null) btnTeachX.Background = Brushes.LightSeaGreen; } catch { }

                int pos = GetSelectedPositionX();
                try
                {
                    if (_shuttle?.ShuttleXMotor != null && _shuttle.ShuttleXMotor.MotorHome)
                    {
                        _shuttle?.ShuttleXMotor?.Teach(pos);
                        try { CleanerControlApp.Vision.Shared.InfoPopup.Show($"Teach (P{pos + 1}) executed.", Window.GetWindow(this), 5); } catch { }
                        //MessageBox.Show($"Teach (P{pos +1}) executed.", "Teach", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        try { CleanerControlApp.Vision.Shared.StatusPopup.Show($"Cannot teach P{pos + 1}: Motor not homed.", Window.GetWindow(this), 5); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    try { CleanerControlApp.Vision.Shared.StatusPopup.Show($"Teach failed: {ex.Message}", Window.GetWindow(this), 5); } catch { }
                    //try { MessageBox.Show($"Teach failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); } catch { }
                }
            }
            catch { }
        }

        private void BtnTeachX_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                _teachTriggered = false;
                // change background to indicate hold
                try { if (btnTeachX != null) btnTeachX.Background = Brushes.Orange; } catch { }
                _teachHoldTimer.Stop();
                _teachHoldTimer.Start();
            }
            catch { }
        }

        private void BtnTeachX_PreviewMouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                _teachHoldTimer.Stop();
                // restore background
                try { if (btnTeachX != null) btnTeachX.Background = Brushes.LightSeaGreen; } catch { }
            }
            catch { }
        }

        private void BtnTeachX_MouseLeave(object? sender, MouseEventArgs e)
        {
            try
            {
                _teachHoldTimer.Stop();
                try { if (btnTeachX != null) btnTeachX.Background = Brushes.LightSeaGreen; } catch { }
            }
            catch { }
        }

        // Z Move/Teach handlers
        private void btnMoveToPZ_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CheckShuttleZMoveStatus())
                {
                    e.Handled = true;
                    return;
                }

                // If clamper is not open, ask user to confirm continuing with Z move
                try
                {
                    if (_shuttle != null && _shuttle.Check_ClamperOpen != true)
                    {
                        bool confirm = CleanerControlApp.Vision.Shared.InfoAskPopup.Ask(
                            "夾爪並未打開，請確認是否要執行移載組Z軸升降? 若下方有卡匣可能造成撞機!!",
                            Window.GetWindow(this));
                        if (!confirm)
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                }
                catch { }

                int pos = GetSelectedPositionZ();
                int speed = GetSelectedSpeedZ();
                _shuttle?.ShuttleZMotor?.MoveToPosition(pos, speed);
                CheckStatus();
            }
            catch { }
        }

        private void btnTeachZ_Click(object sender, RoutedEventArgs e)
        {
            // No action for short click
        }

        // Long-press handlers for Z Teach
        private void TeachHoldTimerZ_Tick(object? sender, EventArgs e)
        {
            try
            {
                _teachHoldTimerZ.Stop();
                _teachTriggeredZ = true;
                try { if (btnTeachZ != null) btnTeachZ.Background = Brushes.LightSeaGreen; } catch { }

                int pos = GetSelectedPositionZ();
                try
                {
                    if (_shuttle?.ShuttleZMotor != null && _shuttle.ShuttleZMotor.MotorHome)
                    {
                        _shuttle?.ShuttleZMotor?.Teach(pos);
                        try { CleanerControlApp.Vision.Shared.InfoPopup.Show($"Teach Z (P{pos + 1}) executed.", Window.GetWindow(this), 5); } catch { }
                        //MessageBox.Show($"Teach Z (P{pos + 1}) executed.", "Teach", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        try { CleanerControlApp.Vision.Shared.StatusPopup.Show($"Cannot teach Z P{pos + 1}: Motor not homed.", Window.GetWindow(this), 5); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    try { CleanerControlApp.Vision.Shared.StatusPopup.Show($"Teach Z failed: {ex.Message}", Window.GetWindow(this), 5); } catch { }
                    //try { MessageBox.Show($"Teach failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); } catch { }
                }
            }
            catch { }
        }

        private void BtnTeachZ_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                _teachTriggeredZ = false;
                // change background to indicate hold
                try { if (btnTeachZ != null) btnTeachZ.Background = Brushes.Orange; } catch { }
                _teachHoldTimerZ.Stop();
                _teachHoldTimerZ.Start();
            }
            catch { }
        }

        private void BtnTeachZ_PreviewMouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                _teachHoldTimerZ.Stop();
                // restore background
                try { if (btnTeachZ != null) btnTeachZ.Background = Brushes.LightSeaGreen; } catch { }
            }
            catch { }
        }

        private void BtnTeachZ_MouseLeave(object? sender, MouseEventArgs e)
        {
            try
            {
                _teachHoldTimerZ.Stop();
                try { if (btnTeachZ != null) btnTeachZ.Background = Brushes.LightSeaGreen; } catch { }
            }
            catch { }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool CheckShuttleXMoveStatus()
        {
            try
            {
                if (_shuttle != null && _shuttle.ShuttleXMotor != null)
                {
                    var status = _shuttle.ShuttleXMotor.MoveStatus;
                    if (!string.IsNullOrEmpty(status))
                    {
                        ShowStatusPopup(status);
                        return false;
                    }
                }
            }
            catch
            {
                // ignore
            }
            return true;
        }

        private bool CheckShuttleZMoveStatus()
        {
            try
            {
                if (_shuttle != null && _shuttle.ShuttleZMotor != null)
                {
                    var status = _shuttle.ShuttleZMotor.MoveStatus;
                    if (!string.IsNullOrEmpty(status))
                    {
                        ShowStatusPopup(status);
                        return false;
                    }
                }
            }
            catch
            {
                // ignore
            }
            return true;
        }

        private void ShowStatusPopup(string status)
        {
            try { CleanerControlApp.Vision.Shared.StatusPopup.Show(status, Window.GetWindow(this),10); } catch { }
        }

        private void CheckStatus()
        {
            var status = _shuttle?.MessageForOperation;
            if (!string.IsNullOrEmpty(status))
            {
                ShowStatusPopup(status);
            }
        }
    }
}
