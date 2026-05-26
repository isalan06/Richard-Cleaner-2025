using System.Windows;

namespace CleanerControlApp.Vision
{
 public partial class ConfirmCloseWindow : Window
 {
 public ConfirmCloseWindow()
 {
 InitializeComponent();
 }

 private void BtnConfirm_Click(object sender, RoutedEventArgs e)
 {
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
