using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CleanerControlApp.Hardwares.Shuttle.Interfaces;
using CleanerControlApp.Hardwares.HeatingTank.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CleanerControlApp.Vision.SystemTemplate
{
    /// <summary>
    /// Template_System_Line.xaml 的互動邏輯
    /// </summary>
    public partial class Template_System_Line : UserControl
    {
        private readonly DispatcherTimer _timer;
        private IShuttle? _shuttle;
        private IHeatingTank? _heatingTank;

        // Dependency properties so XAML ElementName bindings update reliably
        public static readonly DependencyProperty ShuttleAutoProperty = DependencyProperty.Register(
        nameof(ShuttleAuto), typeof(bool), typeof(Template_System_Line), new PropertyMetadata(false));

        public static readonly DependencyProperty ShuttlePausingProperty = DependencyProperty.Register(
        nameof(ShuttlePausing), typeof(bool), typeof(Template_System_Line), new PropertyMetadata(false));

        // ShuttleCassette property
        public static readonly DependencyProperty ShuttleCassetteProperty = DependencyProperty.Register(
        nameof(ShuttleCassette), typeof(bool), typeof(Template_System_Line), new PropertyMetadata(false));

        // ShuttleIdle property
        public static readonly DependencyProperty ShuttleIdleProperty = DependencyProperty.Register(
        nameof(ShuttleIdle), typeof(bool), typeof(Template_System_Line), new PropertyMetadata(false));

        // New: ShuttleInitialized and ShuttleInitializing properties
        public static readonly DependencyProperty ShuttleInitializedProperty = DependencyProperty.Register(
        nameof(ShuttleInitialized), typeof(bool), typeof(Template_System_Line), new PropertyMetadata(false));

        public static readonly DependencyProperty ShuttleInitializingProperty = DependencyProperty.Register(
        nameof(ShuttleInitializing), typeof(bool), typeof(Template_System_Line), new PropertyMetadata(false));

        // New: ShuttleHasWarning property
        public static readonly DependencyProperty ShuttleHasWarningProperty = DependencyProperty.Register(
        nameof(ShuttleHasWarning), typeof(bool), typeof(Template_System_Line), new PropertyMetadata(false));

        // New: ShuttleHasAlarm property
        public static readonly DependencyProperty ShuttleHasAlarmProperty = DependencyProperty.Register(
        nameof(ShuttleHasAlarm), typeof(bool), typeof(Template_System_Line), new PropertyMetadata(false));

        // New: HeatingTankAuto property
        public static readonly DependencyProperty HeatingTankAutoProperty = DependencyProperty.Register(
        nameof(HeatingTankAuto), typeof(bool), typeof(Template_System_Line), new PropertyMetadata(false));

        // New: HeatingTankPausing property
        public static readonly DependencyProperty HeatingTankPausingProperty = DependencyProperty.Register(
        nameof(HeatingTankPausing), typeof(bool), typeof(Template_System_Line), new PropertyMetadata(false));

        // New: HeatingTankInitializedStatus property
        public static readonly DependencyProperty HeatingTankInitializedStatusProperty = DependencyProperty.Register(
        nameof(HeatingTankInitializedStatus), typeof(bool), typeof(Template_System_Line), new PropertyMetadata(false));

        // New: HeatingTankHasWarning property
        public static readonly DependencyProperty HeatingTankHasWarningProperty = DependencyProperty.Register(
        nameof(HeatingTankHasWarning), typeof(bool), typeof(Template_System_Line), new PropertyMetadata(false));

        // New: HeatingTankHasAlarm property
        public static readonly DependencyProperty HeatingTankHasAlarmProperty = DependencyProperty.Register(
        nameof(HeatingTankHasAlarm), typeof(bool), typeof(Template_System_Line), new PropertyMetadata(false));

        public bool ShuttleAuto
        {
            get => (bool)GetValue(ShuttleAutoProperty);
            set => SetValue(ShuttleAutoProperty, value);
        }

        public bool ShuttlePausing
        {
            get => (bool)GetValue(ShuttlePausingProperty);
            set => SetValue(ShuttlePausingProperty, value);
        }

        public bool ShuttleCassette
        {
            get => (bool)GetValue(ShuttleCassetteProperty);
            set => SetValue(ShuttleCassetteProperty, value);
        }

        public bool ShuttleIdle
        {
            get => (bool)GetValue(ShuttleIdleProperty);
            set => SetValue(ShuttleIdleProperty, value);
        }

        public bool ShuttleInitialized
        {
            get => (bool)GetValue(ShuttleInitializedProperty);
            set => SetValue(ShuttleInitializedProperty, value);
        }

        public bool ShuttleInitializing
        {
            get => (bool)GetValue(ShuttleInitializingProperty);
            set => SetValue(ShuttleInitializingProperty, value);
        }

        public bool ShuttleHasWarning
        {
            get => (bool)GetValue(ShuttleHasWarningProperty);
            set => SetValue(ShuttleHasWarningProperty, value);
        }

        public bool ShuttleHasAlarm
        {
            get => (bool)GetValue(ShuttleHasAlarmProperty);
            set => SetValue(ShuttleHasAlarmProperty, value);
        }

        public bool HeatingTankAuto
        {
            get => (bool)GetValue(HeatingTankAutoProperty);
            set => SetValue(HeatingTankAutoProperty, value);
        }

        public bool HeatingTankPausing
        {
            get => (bool)GetValue(HeatingTankPausingProperty);
            set => SetValue(HeatingTankPausingProperty, value);
        }

        public bool HeatingTankInitializedStatus
        {
            get => (bool)GetValue(HeatingTankInitializedStatusProperty);
            set => SetValue(HeatingTankInitializedStatusProperty, value);
        }

        public bool HeatingTankHasWarning
        {
            get => (bool)GetValue(HeatingTankHasWarningProperty);
            set => SetValue(HeatingTankHasWarningProperty, value);
        }

        public bool HeatingTankHasAlarm
        {
            get => (bool)GetValue(HeatingTankHasAlarmProperty);
            set => SetValue(HeatingTankHasAlarmProperty, value);
        }

        public Template_System_Line()
        {
            InitializeComponent();

            try
            {
                _shuttle = App.AppHost?.Services.GetService<IShuttle>();
            }
            catch { _shuttle = null; }

            try
            {
                _heatingTank = App.AppHost?.Services.GetService<IHeatingTank>();
            }
            catch { _heatingTank = null; }

            _timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = System.TimeSpan.FromMilliseconds(250) };
            _timer.Tick += Timer_Tick;
            Loaded += (s, e) => { DataContext = this; _timer.Start(); };
            Unloaded += (s, e) => { _timer.Stop(); };

            // initial update
            UpdateFromShuttle();
        }

        private void Timer_Tick(object? sender, System.EventArgs e)
        {
            UpdateFromShuttle();
        }

        private void UpdateFromShuttle()
        {
            try
            {
                if (_shuttle == null)
                {
                    try { _shuttle = App.AppHost?.Services.GetService<IShuttle>(); } catch { _shuttle = null; }
                }
                if (_heatingTank == null)
                {
                    try { _heatingTank = App.AppHost?.Services.GetService<IHeatingTank>(); } catch { _heatingTank = null; }
                }

                if (_shuttle != null)
                {
                    ShuttleAuto = _shuttle.Auto;
                    ShuttlePausing = _shuttle.Pausing;
                    ShuttleCassette = _shuttle.Cassette;
                    ShuttleIdle = _shuttle.Idle;
                    ShuttleInitialized = _shuttle.Initialized;
                    ShuttleInitializing = _shuttle.Initializing;
                    ShuttleHasWarning = _shuttle.HasWarning;
                    ShuttleHasAlarm = _shuttle.HasAlarm;
                }
                else
                {
                    ShuttleAuto = false;
                    ShuttlePausing = false;
                    ShuttleCassette = false;
                    ShuttleIdle = false;
                    ShuttleInitialized = false;
                    ShuttleInitializing = false;
                    ShuttleHasWarning = false;
                    ShuttleHasAlarm = false;
                }

                // Heating tank properties
                if (_heatingTank != null)
                {
                    HeatingTankAuto = _heatingTank.Auto;
                    HeatingTankPausing = _heatingTank.Pausing;
                    HeatingTankInitializedStatus = _heatingTank.Initialized;
                    HeatingTankHasWarning = _heatingTank.HasWarning;
                    HeatingTankHasAlarm = _heatingTank.HasAlarm;
                }
                else
                {
                    HeatingTankAuto = false;
                    HeatingTankPausing = false;
                    HeatingTankInitializedStatus = false;
                    HeatingTankHasWarning = false;
                    HeatingTankHasAlarm = false;
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
