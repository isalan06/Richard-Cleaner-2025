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
using CleanerControlApp.Hardwares.Sink.Interfaces;
using CleanerControlApp.Hardwares.SoakingTank.Interfaces;
using CleanerControlApp.Hardwares.DryingTank.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_System.xaml 的互動邏輯
    /// </summary>
    public partial class Template_System : UserControl
    {
        private readonly ISink? _sink;
        private readonly ISoakingTank? _soakingTank;
        private readonly IDryingTank[]? _dryingTanks;
        private readonly DispatcherTimer _timer;

        public Template_System()
        {
            InitializeComponent();

            try
            {
                if (App.AppHost != null)
                {
                    _sink = App.AppHost.Services.GetService<ISink>();
                    _soakingTank = App.AppHost.Services.GetService<ISoakingTank>();
                    // drying tanks resolved as array service
                    _dryingTanks = App.AppHost.Services.GetService<IDryingTank[]>();
                }
            }
            catch { }

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += Timer_Tick;

            Loaded += (s, e) => _timer.Start();
            Unloaded += (s, e) => _timer.Stop();

            UpdateButtons();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            try
            {
                UpdateButtonVisual(Btn_Sink_Pass, _sink?.ModulePass == true);
                UpdateButtonVisual(Btn_Soaking_Pass, _soakingTank?.ModulePass == true);
                UpdateButtonVisual(Btn_Dry1_Pass, _dryingTanks != null && _dryingTanks.Length >0 && _dryingTanks[0].ModulePass == true);
                UpdateButtonVisual(Btn_Dry2_Pass, _dryingTanks != null && _dryingTanks.Length >1 && _dryingTanks[1].ModulePass == true);
            }
            catch { }
        }

        private void UpdateButtonVisual(Button? btn, bool isPass)
        {
            if (btn == null) return;
            if (isPass)
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(0xD9,0xF0,0xFF)); // light blue
            }
            else
            {
                btn.ClearValue(Button.BackgroundProperty);
            }
        }

        private void Btn_Sink_Pass_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_sink != null)
                {
                    _sink.ModulePass = !_sink.ModulePass;
                    UpdateButtons();
                }
            }
            catch { }
        }

        private void Btn_Soaking_Pass_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_soakingTank != null)
                {
                    _soakingTank.ModulePass = !_soakingTank.ModulePass;
                    UpdateButtons();
                }
            }
            catch { }
        }

        private void Btn_Dry1_Pass_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length >0 && _dryingTanks[0] != null)
                {
                    _dryingTanks[0].ModulePass = !_dryingTanks[0].ModulePass;
                    UpdateButtons();
                }
            }
            catch { }
        }

        private void Btn_Dry2_Pass_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dryingTanks != null && _dryingTanks.Length >1 && _dryingTanks[1] != null)
                {
                    _dryingTanks[1].ModulePass = !_dryingTanks[1].ModulePass;
                    UpdateButtons();
                }
            }
            catch { }
        }
    }
}
