using CleanerControlApp.Hardwares.Shuttle.Interfaces;
using System.Windows.Controls;

namespace CleanerControlApp.Vision.Developer.Module
{
    public partial class DevModuleShuttleView : UserControl
    {
        private IShuttle? _shuttle;
        public DevModuleShuttleView()
        {
            InitializeComponent();

            if (App.AppHost != null)
            {
                try
                {
                    _shuttle = App.AppHost.Services.GetService(typeof(IShuttle)) as IShuttle;
                }
                catch { }
            }
        }

        private void Btn_Init_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _shuttle?.ModuleReset(); } catch { }
        }

        private void Btn_Auto_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _shuttle?.AutoStart(); } catch { }
        }

        private void Btn_Stop_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _shuttle?.AutoStop(); } catch { }
        }

        private void Btn_Pause_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _shuttle?.AutoPause(); } catch { }
        }

        private void Btn_AlarmStop_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _shuttle?.AlarmStop(); } catch { }
        }

        private void Btn_MotorPass_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _shuttle?.SimMotorPass(); } catch { }
        }

        private void Btn_ClamerPass_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _shuttle?.SimClamperPass(); } catch { }
        }
    }
}