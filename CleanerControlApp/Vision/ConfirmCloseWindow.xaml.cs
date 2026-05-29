using System.Windows;
using CleanerControlApp.Hardwares;
using System.Threading.Tasks;

namespace CleanerControlApp.Vision
{
    public partial class ConfirmCloseWindow : Window
    {
        public ConfirmCloseWindow()
        {
            InitializeComponent();
        }

        private async void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            try
            {
                var hw = App.AppHost?.Services.GetService(typeof(HardwareManager)) as HardwareManager;
                if (hw != null)
                {
                    await hw.ModuleClose().ConfigureAwait(false);
                }
            }
            catch { }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
