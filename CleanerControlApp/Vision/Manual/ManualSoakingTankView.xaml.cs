using System.Windows.Controls;
using CleanerControlApp.Vision.Template;

namespace CleanerControlApp.Vision.Manual
{
 public partial class ManualSoakingTankView : UserControl
 {
 public ManualSoakingTankView()
 {
 InitializeComponent();

 try
 {
 // create and place the template control at runtime using FindName to avoid generated field dependency
 var ctrl = new Template_SoakingTank();
 var host = this.FindName("TemplateHost") as ContentControl;
 if (host != null)
 {
 host.Content = ctrl;
 }
 }
 catch
 {
 // ignore if designer can't create control
 }
 }
 }
}