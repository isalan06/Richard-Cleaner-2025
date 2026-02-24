using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CleanerControlApp.Vision.Developer
{
 /// <summary>
 /// Interaction logic for DeveloperParameterView.xaml
 /// </summary>
 public partial class DeveloperParameterView : UserControl
 {
 public DeveloperParameterView()
 {
 InitializeComponent();
 }

 private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
 {
 // Read appsettings.json
 try
 {
 var builder = new ConfigurationBuilder()
 .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
 var config = builder.Build();
 var title = config.GetSection("AppSettings").GetValue<string>("Title");
 // You can use the loaded settings as needed; for now set DataContext or show in Tooltip
 this.ToolTip = title;
 }
 catch
 {
 // ignore
 }

 // Load default view (DevCommParameterView)
 LoadDevCommView();
 }

 private void Btn_CommunicationParameter_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 LoadDevCommView();
 }

 private void LoadDevCommView()
 {
 try
 {
 // Create instance of the DevCommParameterView user control and set to ContentControl
 var view = new DevCommParameterView();
 RightContent.Content = view;
 }
 catch
 {
 // ignore
 }
 }
 }
}
