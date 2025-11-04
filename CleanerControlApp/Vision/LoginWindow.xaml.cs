using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using CleanerControlApp.Modules.UserManagement.Services;


namespace CleanerControlApp.Vision
{
    /// <summary>
    /// LoginWindow.xaml 的互動邏輯
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly UserManager _userManager;
        public LoginWindow(UserManager userManager)
        {
            InitializeComponent();
            _userManager = userManager;
        }


        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (_userManager.Login(username, password))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorText.Text = "帳號或密碼錯誤，請重新輸入。";
            }
        }
    }
}
