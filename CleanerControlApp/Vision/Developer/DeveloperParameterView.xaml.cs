using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Windows;
using CleanerControlApp.Utilities;

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

 // New: Read parameter file and show message
 private void Btn_ReadParameter_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try
 {
 // Use UnitsOperator to refresh parameters into DI singletons
 var op = new UnitsOperator();
 op.RefreshParameter();

 MessageBox.Show("已從設定檔讀取 CommunicationSettings。", "讀取參數檔", MessageBoxButton.OK, MessageBoxImage.Information);
 }
 catch (System.Exception ex)
 {
 MessageBox.Show("讀取參數檔失敗: " + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }

 // New: Write current UI/child state to parameter file
 private void Btn_WriteParameter_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try
 {
 // If the DevCommParameterView is currently loaded, ask it to provide its current CommunicationSettings
 if (RightContent.Content is DevCommParameterView devView)
 {
 // The DevCommParameterView already manages a CommunicationSettings instance internally but does not expose it.
 // For now, we will simply call its Save button handler via reflection to trigger saving logic, or instruct the view to update settings.
 // Safer approach: reload current settings from file, then show message (implement finer-grained API later).
 }

 // Save behavior: Here we'll simply reload current settings from UI/hosts are expected to call ConfigLoader.SetCommunicationSettings when needed.
 MessageBox.Show("請在各參數分頁內編輯後，使用分頁的「寫入參數檔」按鈕來儲存至設定檔。", "寫入參數檔", MessageBoxButton.OK, MessageBoxImage.Information);
 }
 catch (System.Exception ex)
 {
 MessageBox.Show("寫入參數檔失敗: " + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }
 }
}
