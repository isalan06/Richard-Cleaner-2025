using System.Windows;
using System.Windows.Controls;
using CleanerControlApp.Modules.UserManagement.Models;
using CleanerControlApp.Modules.UserManagement.Services;

namespace CleanerControlApp.Vision.Popup
{
 public partial class EditUserPopup : Window
 {
 private UserInfo _user;
 public EditUserPopup(UserInfo user)
 {
 InitializeComponent();
 _user = user;
 txtUserName.Text = user.Name;

 // select current role
 switch (user.CurrentUserRole)
 {
 case UserRole.Administrator: cboRole.SelectedIndex =0; break;
 case UserRole.Engineer: cboRole.SelectedIndex =1; break;
 default: cboRole.SelectedIndex =2; break;
 }

 btnOk.Click += BtnOk_Click;
 btnCancel.Click += (s, e) => this.DialogResult = false;
 }

 private void BtnOk_Click(object sender, RoutedEventArgs e)
 {
 string pwd = pwdNew.Password ?? string.Empty;
 string confirm = pwdConfirm.Password ?? string.Empty;

 if (string.IsNullOrEmpty(pwd))
 {
 MessageBox.Show("請輸入密碼", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
 return;
 }
 if (pwd != confirm)
 {
 MessageBox.Show("兩次密碼輸入不相同", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
 return;
 }

 UserRole role = UserRole.Operator;
 var sel = cboRole.SelectedItem as ComboBoxItem;
 if (sel != null)
 {
 var txt = sel.Content?.ToString();
 if (txt == "Administrator") role = UserRole.Administrator;
 else if (txt == "Engineer") role = UserRole.Engineer;
 else role = UserRole.Operator;
 }

 bool ok = UserRepository.UpdatePasswordAndRole(_user.Id, pwd, role);
 if (!ok)
 {
 MessageBox.Show("更新使用者失敗", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
 return;
 }

 this.DialogResult = true;
 }
 }
}
