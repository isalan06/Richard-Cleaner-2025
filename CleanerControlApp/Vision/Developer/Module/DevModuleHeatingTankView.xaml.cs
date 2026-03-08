using CleanerControlApp.Hardwares.HeatingTank.Interfaces;
using System.Windows.Controls;

namespace CleanerControlApp.Vision.Developer.Module
{
    public partial class DevModuleHeatingTankView : UserControl
    {
        private IHeatingTank? _heatingTank;
        public DevModuleHeatingTankView()
        {
            InitializeComponent();

            if (App.AppHost != null)
            {
                try
                {
                    _heatingTank = App.AppHost.Services.GetService(typeof(IHeatingTank)) as IHeatingTank;
                }
                catch { }
            }
        }

        private void Btn_Init_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _heatingTank?.ModuleReset(); } catch { }
        }

        private void Btn_Auto_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _heatingTank?.AutoStart(); } catch { }
        }

        private void Btn_Stop_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _heatingTank?.AutoStop(); } catch { }
        }

        private void Btn_Pause_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _heatingTank?.AutoPause(); } catch { }
        }

        private void Btn_AlarmStop_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _heatingTank?.AlarmStop(); } catch { }
        }

        private void Btn_SimTemp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _heatingTank?.SimTemperature(); } catch { }
        }

        private void Btn_SimHigh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _heatingTank?.SimFrequency(2); } catch { }
        }

        private void Btn_SimLow_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _heatingTank?.SimFrequency(1); } catch { }
        }

        private void Btn_SimZero_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _heatingTank?.SimFrequency(0); } catch { }
        }

        private void Btn_RequestWater_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_heatingTank != null)
                    _heatingTank.HS_RequestWater = !_heatingTank.HS_RequestWater;
            }
            catch { }
        }
    }
}