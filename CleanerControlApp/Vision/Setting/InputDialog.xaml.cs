using System.Windows;

namespace CleanerControlApp.Vision.SettingViews
{
 public partial class InputDialog : Window
 {
 public string Result { get; private set; }

 public InputDialog(string defaultName = "")
 {
 InitializeComponent();
 TxtName.Text = defaultName;
 TxtName.SelectAll();
 TxtName.Focus();
 }

 private void BtnOk_Click(object sender, RoutedEventArgs e)
 {
 Result = TxtName.Text?.Trim() ?? string.Empty;
 this.DialogResult = true;
 this.Close();
 }

 private void BtnCancel_Click(object sender, RoutedEventArgs e)
 {
 this.DialogResult = false;
 this.Close();
 }
 }
}