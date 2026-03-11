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
using CleanerControlApp.Utilities.Log;
using Microsoft.Extensions.Logging;
using CleanerControlApp.Hardwares;


namespace CleanerControlApp.Vision
{
    /// <summary>
    /// LoginWindow.xaml 的互動邏輯
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly UserManager _userManager;
        private readonly ILogger<LoginWindow> _logger;
        private readonly HardwareManager _hardwareManager;
        public LoginWindow(UserManager userManager, ILogger<LoginWindow> logger, HardwareManager hardwareManager)
        {
            InitializeComponent();
            _userManager = userManager;
            _logger = logger;
            _hardwareManager = hardwareManager;

            // Pre-fill credentials for testing to avoid repeatedly typing them.
            // TODO: Remove these defaults before production.
            UsernameTextBox.Text = "supervisor";
            PasswordBox.Password = "9527";
        }


        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;
            _logger.LogTrace($"開始進行登入流程");

            if (_userManager.Login(username, password))
            {
                _logger.LogInformation($"使用者 '{username}' 登入成功，角色：{_userManager.UserInfo?.CurrentUserRole}");
                OperateLog.Log("登入成功", $"使用者 '{username}' 登入成功，角色：{_userManager.UserInfo?.CurrentUserRole}");
                DialogResult = true;
                // If the application is not configured to bypass hardware checks, ensure communication is connected.
                //if (UserManager.CanPassCheck)
                //{
                //    _hardwareManager.CommunicationConnect(false);
                //    _hardwareManager.ModuleRunning(false);
                //}
                Close();
            }
            else
            {
                _logger.LogWarning($"使用者 '{username}' 登入失敗。");
                OperateLog.Log("登入失敗", username, "帳號或密碼錯誤");
                ErrorText.Text = "帳號或密碼錯誤，請重新輸入。";
            }
        }
    }
}
