using CleanerControlApp.Hardwares.Sink.Interfaces;
using System.Windows.Controls;

namespace CleanerControlApp.Vision.Developer.Module
{
    public partial class DevModuleSinkView : UserControl
    {
        private ISink? _sink;
        public DevModuleSinkView()
        {
            InitializeComponent();

            if (App.AppHost != null)
            {
                try
                {
                    _sink = App.AppHost.Services.GetService(typeof(ISink)) as ISink;
                }
                catch { }
            }
        }

        private void Btn_Init_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _sink?.ModuleReset(); } catch { }
        }

        private void Btn_Auto_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _sink?.AutoStart(); } catch { }
        }

        private void Btn_Stop_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _sink?.AutoStop(); } catch { }
        }

        private void Btn_Pause_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _sink?.AutoPause(); } catch { }
        }

        private void Btn_AlarmStop_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _sink?.AlarmStop(); } catch { }
        }

        private void Btn_HiPressure_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _sink?.SimHiPressure(); } catch { }
        }

        private void Btn_MotorPass_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { _sink?.SimMotorPass(); } catch { }
        }

        private void Btn_Pick_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_sink != null)
                    _sink.HS_ClamperPickFinished = !_sink.HS_ClamperPickFinished;
            }
            catch { }
        }

        private void Btn_Place_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_sink != null)
                    _sink.HS_ClamperPlaceFinished = !_sink.HS_ClamperPlaceFinished;
            }
            catch { }
        }

        private void Btn_Move_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_sink != null)
                    _sink.HS_ClamperMoving = !_sink.HS_ClamperMoving;
            }
            catch { }
        }
    }
}