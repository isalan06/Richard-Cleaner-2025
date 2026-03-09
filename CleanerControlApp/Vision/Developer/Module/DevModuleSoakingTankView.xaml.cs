using CleanerControlApp.Hardwares.SoakingTank.Interfaces;
using System.Windows.Controls;

namespace CleanerControlApp.Vision.Developer.Module
{
 public partial class DevModuleSoakingTankView : UserControl
 {
 private ISoakingTank? _soakingTank;
 public DevModuleSoakingTankView()
 {
 InitializeComponent();

 if (App.AppHost != null)
 {
 try
 {
 _soakingTank = App.AppHost.Services.GetService(typeof(ISoakingTank)) as ISoakingTank;
 }
 catch { }
 }
 }

 private void Btn_Init_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try { _soakingTank?.ModuleReset(); } catch { }
 }

 private void Btn_Auto_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try { _soakingTank?.AutoStart(); } catch { }
 }

 private void Btn_Stop_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try { _soakingTank?.AutoStop(); } catch { }
 }

 private void Btn_Pause_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try { _soakingTank?.AutoPause(); } catch { }
 }

 private void Btn_AlarmStop_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try { _soakingTank?.AlarmStop(); } catch { }
 }

 private void Btn_MotorPass_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try { _soakingTank?.SimMotorPass(); } catch { }
 }

 private void Btn_Pick_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try
 {
 if (_soakingTank != null)
 _soakingTank.HS_ClamperPickFinished = !_soakingTank.HS_ClamperPickFinished;
 }
 catch { }
 }

 private void Btn_Place_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try
 {
 if (_soakingTank != null)
 _soakingTank.HS_ClamperPlaceFinished = !_soakingTank.HS_ClamperPlaceFinished;
 }
 catch { }
 }

 private void Btn_Move_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try
 {
 if (_soakingTank != null)
 _soakingTank.HS_ClamperMoving = !_soakingTank.HS_ClamperMoving;
 }
 catch { }
 }
 }
}