using System.Windows.Controls;
using CleanerControlApp.Modules.UserManagement.Services;
using CleanerControlApp.Modules.UserManagement.Models;
using System.Windows;
using System.Linq;
using System.Windows.Input;
using CleanerControlApp.Vision.Popup;
using System.Windows.Media; // ← 新增這一行

namespace CleanerControlApp.Vision
{
    /// <summary>
    /// UserView.xaml 的互動邏輯
    /// </summary>
    public partial class UserView : UserControl
    {
        public UserView()
        {
            InitializeComponent();

            // Load users and bind to DataGrid
            RefreshUserList();

            // Set default role to Operator
            cboRole.SelectedIndex =2; // Operator

            // Hook up password toggle handlers
            btnToggleNewPassword.Click += BtnToggleNewPassword_Click;
            btnToggleConfirmPassword.Click += BtnToggleConfirmPassword_Click;

            // Sync visible textboxes when password changes
            pwdNewPassword.PasswordChanged += (s, e) => {
                if (txtNewPasswordVisible.Visibility == Visibility.Visible)
                    txtNewPasswordVisible.Text = pwdNewPassword.Password;
            };
            pwdConfirmPassword.PasswordChanged += (s, e) => {
                if (txtConfirmPasswordVisible.Visibility == Visibility.Visible)
                    txtConfirmPasswordVisible.Text = pwdConfirmPassword.Password;
            };

            // Keep PasswordBox updated when visible text changes
            txtNewPasswordVisible.TextChanged += (s, e) => {
                if (txtNewPasswordVisible.Visibility == Visibility.Visible)
                    pwdNewPassword.Password = txtNewPasswordVisible.Text;
            };
            txtConfirmPasswordVisible.TextChanged += (s, e) => {
                if (txtConfirmPasswordVisible.Visibility == Visibility.Visible)
                    pwdConfirmPassword.Password = txtConfirmPasswordVisible.Text;
            };
        }

        private void UsersDataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Select the row under the mouse on right-click so context menu operates on it
            var dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            if (dep is DataGridRow row)
            {
                UsersDataGrid.SelectedItem = row.Item;
            }
        }

        private void ChangeUser_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var user = UsersDataGrid.SelectedItem as UserInfo;
            if (user == null) return;

            var popup = new EditUserPopup(user)
            {
                Owner = Window.GetWindow(this)
            };
            bool? res = popup.ShowDialog();
            if (res == true)
            {
                RefreshUserList();
            }
        }

        private void DeleteUser_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var user = UsersDataGrid.SelectedItem as UserInfo;
            if (user == null) return;

            var msg = $"確定要刪除使用者 '{user.Name}' 嗎?";
            var r = MessageBox.Show(msg, "刪除使用者", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r == MessageBoxResult.Yes)
            {
                bool ok = UserRepository.DeleteUser(user.Id);
                if (!ok)
                {
                    MessageBox.Show("刪除失敗", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    RefreshUserList();
                }
            }
        }

        private void BtnToggleNewPassword_Click(object sender, RoutedEventArgs e)
        {
            if (txtNewPasswordVisible.Visibility == Visibility.Collapsed)
            {
                txtNewPasswordVisible.Text = pwdNewPassword.Password;
                txtNewPasswordVisible.Visibility = Visibility.Visible;
                pwdNewPassword.Visibility = Visibility.Collapsed;
                btnToggleNewPassword.Content = "隱藏";
            }
            else
            {
                pwdNewPassword.Password = txtNewPasswordVisible.Text;
                txtNewPasswordVisible.Visibility = Visibility.Collapsed;
                pwdNewPassword.Visibility = Visibility.Visible;
                btnToggleNewPassword.Content = "顯示";
            }
        }

        private void BtnToggleConfirmPassword_Click(object sender, RoutedEventArgs e)
        {
            if (txtConfirmPasswordVisible.Visibility == Visibility.Collapsed)
            {
                txtConfirmPasswordVisible.Text = pwdConfirmPassword.Password;
                txtConfirmPasswordVisible.Visibility = Visibility.Visible;
                pwdConfirmPassword.Visibility = Visibility.Collapsed;
                btnToggleConfirmPassword.Content = "隱藏";
            }
            else
            {
                pwdConfirmPassword.Password = txtConfirmPasswordVisible.Text;
                txtConfirmPasswordVisible.Visibility = Visibility.Collapsed;
                pwdConfirmPassword.Visibility = Visibility.Visible;
                btnToggleConfirmPassword.Content = "顯示";
            }
        }

        private void RefreshUserList()
        {
            var users = UserRepository.GetAllUsers();
            UsersDataGrid.ItemsSource = users;
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            string name = txtNewUserName.Text?.Trim() ?? string.Empty;
            string pwd = pwdNewPassword.Password ?? string.Empty;
            string confirm = pwdConfirmPassword.Password ?? string.Empty;

            // Validation
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("請輸入使用者名稱", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (name.ToLower() == "supervisor")
            {
                MessageBox.Show("此使用者名稱不允許使用", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existing = UserRepository.GetAllUsers();
            if (existing.Any(u => string.Equals(u.Name, name, System.StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("使用者名稱已存在", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

            // Determine role from ComboBox
            UserRole role = UserRole.Operator; // default
            var selected = cboRole.SelectedItem as ComboBoxItem;
            if (selected != null)
            {
                var text = selected.Content?.ToString();
                if (text == "Administrator") role = UserRole.Administrator;
                else if (text == "Engineer") role = UserRole.Engineer;
                else role = UserRole.Operator;
            }

            try
            {
                var newUser = new UserInfo(0, name, pwd, role);
                UserRepository.AddUser(newUser);
                MessageBox.Show("新增使用者成功", "完成", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh list and clear inputs
                RefreshUserList();
                txtNewUserName.Text = string.Empty;
                pwdNewPassword.Password = string.Empty;
                pwdConfirmPassword.Password = string.Empty;
                cboRole.SelectedIndex =2;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"新增使用者失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
