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
using Microsoft.Extensions.Logging;


namespace CleanerControlApp.Vision
{
    /// <summary>
    /// LoginWindow.xaml 的互動邏輯
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly UserManager _userManager;
        private readonly ILogger<LoginWindow> _logger;
        public LoginWindow(UserManager userManager, ILogger<LoginWindow> logger)
        {
            InitializeComponent();
            _userManager = userManager;
            _logger = logger;
        }


        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;
            _logger.LogTrace($"開始進行登入流程");

            if (_userManager.Login(username, password))
            {
                _logger.LogInformation($"使用者 '{username}' 登入成功，角色：{_userManager.UserInfo?.CurrentUserRole}");
                DialogResult = true;
                Close();
            }
            else
            {
                _logger.LogWarning($"使用者 '{username}' 登入失敗。");
                ErrorText.Text = "帳號或密碼錯誤，請重新輸入。";
            }
        }
    }
}
