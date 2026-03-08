using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CleanerControlApp.Hardwares;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CleanerControlApp.Hardwares.HeatingTank.Interfaces;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_Tank_Heating.xaml 的互動邏輯
    /// </summary>
    public partial class Template_Tank_Heating : UserControl, INotifyPropertyChanged
    {
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private double _origLeft;
        private double _origTop;

        private readonly IHeatingTank? _heatingTank;
        private readonly DispatcherTimer _timer;

        private bool _requestWater;
        private bool _autoStatus;
        private bool _pauseStatus;
        private bool _initializedStatus;
        private bool _warningStatus;
        private bool _alarmStatus;
        private bool _act;

        // PV and tank sensor/temperature flags
        private double _pv;
        private bool _highTC;
        private bool _lowTC;
        private bool _tankHH;
        private bool _tankH;
        private bool _tankL;
        private bool _tankLL;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Template_Tank_Heating()
        {
            InitializeComponent();

            try { _heatingTank = App.AppHost?.Services.GetService<IHeatingTank>(); }
            catch { _heatingTank = null; }

            _timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(250) };
            _timer.Tick += Timer_Tick;
            Loaded += (s, e) => _timer.Start();
            Unloaded += (s, e) => _timer.Stop();

            UpdateFromHeatingTank();
        }

        public bool RequestWater { get => _requestWater; private set { if (_requestWater != value) { _requestWater = value; OnPropertyChanged(); } } }
        public bool AutoStatus { get => _autoStatus; private set { if (_autoStatus != value) { _autoStatus = value; OnPropertyChanged(); } } }
        public bool PauseStatus { get => _pauseStatus; private set { if (_pauseStatus != value) { _pauseStatus = value; OnPropertyChanged(); } } }
        public bool InitializedStatus { get => _initializedStatus; private set { if (_initializedStatus != value) { _initializedStatus = value; OnPropertyChanged(); } } }
        public bool WarningStatus { get => _warningStatus; private set { if (_warningStatus != value) { _warningStatus = value; OnPropertyChanged(); } } }
        public bool AlarmStatus { get => _alarmStatus; private set { if (_alarmStatus != value) { _alarmStatus = value; OnPropertyChanged(); } } }
        public bool Act { get => _act; private set { if (_act != value) { _act = value; OnPropertyChanged(); } } }

        public double PV { get => _pv; private set { if (Math.Abs(_pv - value) >0.0001) { _pv = value; OnPropertyChanged(); } } }
        public bool HighTC { get => _highTC; private set { if (_highTC != value) { _highTC = value; OnPropertyChanged(); } } }
        public bool LowTC { get => _lowTC; private set { if (_lowTC != value) { _lowTC = value; OnPropertyChanged(); } } }
        public bool TankHH { get => _tankHH; private set { if (_tankHH != value) { _tankHH = value; OnPropertyChanged(); } } }
        public bool TankH { get => _tankH; private set { if (_tankH != value) { _tankH = value; OnPropertyChanged(); } } }
        public bool TankL { get => _tankL; private set { if (_tankL != value) { _tankL = value; OnPropertyChanged(); } } }
        public bool TankLL { get => _tankLL; private set { if (_tankLL != value) { _tankLL = value; OnPropertyChanged(); } } }

        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void Timer_Tick(object? sender, EventArgs e) => UpdateFromHeatingTank();

        private void UpdateFromHeatingTank()
        {
            try
            {
                if (_heatingTank != null)
                {
                    RequestWater = _heatingTank.HS_RequestWater;
                    AutoStatus = _heatingTank.Auto;
                    PauseStatus = _heatingTank.Pausing;
                    InitializedStatus = _heatingTank.Initialized;
                    WarningStatus = _heatingTank.HasWarning;
                    AlarmStatus = _heatingTank.HasAlarm;
                    Act = _heatingTank.Heating;

                    PV = _heatingTank.PV_Value;
                    HighTC = _heatingTank.HighTemperature;
                    LowTC = _heatingTank.LowTemperature;
                    TankHH = _heatingTank.Sensor_Liquid_HH;
                    TankH = _heatingTank.Sensor_Liquid_H;
                    TankL = _heatingTank.Sensor_Liquid_L;
                    TankLL = _heatingTank.Sensor_Liquid_LL;
                }
                else
                {
                    RequestWater = AutoStatus = PauseStatus = InitializedStatus = WarningStatus = AlarmStatus = Act = false;
                    PV =0.0;
                    HighTC = LowTC = TankHH = TankH = TankL = TankLL = false;
                }
            }
            catch
            {
                // ignore
            }
        }

        private void Group_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement el)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(Canvas_TankHeating);
                _origLeft = Canvas.GetLeft(tankGroup);
                _origTop = Canvas.GetTop(tankGroup);
                el.CaptureMouse();
            }
        }

        private void Group_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point pos = e.GetPosition(Canvas_TankHeating);
                double dx = pos.X - _dragStartPoint.X;
                double dy = pos.Y - _dragStartPoint.Y;
                Canvas.SetLeft(tankGroup, _origLeft + dx);
                Canvas.SetTop(tankGroup, _origTop + dy);
            }
        }

        private void Group_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                if (sender is UIElement el)
                    el.ReleaseMouseCapture();
            }
        }
    }
}
