using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Extensions.Logging;
using CleanerControlApp.Vision;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using CleanerControlApp.Modules.UserManagement.Services;
using Microsoft.Extensions.DependencyInjection;
using CleanerControlApp.Modules.UserManagement.Models;

namespace CleanerControlApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger; // Logger 欄位
        private readonly IConfiguration _configuration;
        private readonly UserManager _userManager;
        private readonly IServiceProvider _services;

        /// <summary>
        /// 建構式，注入 Logger
        /// </summary>
        /// <param name="logger"></param>
        public MainWindow(ILogger<MainWindow> logger, IConfiguration configuration, UserManager userManager, IServiceProvider services)
        {
            InitializeComponent();
            _logger = logger; // 注入的 Logger
            _configuration = configuration;
            _userManager = userManager;
            _services = services;
        }

        /// <summary>
        /// 視窗內容呈現完成後觸發
        /// </summary>
        /// <param name="e"></param>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            //_logger.LogInformation("MainWindow Show.");
            //Console.WriteLine("MainWindow Show.");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set version in status bar from assembly information
            try
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var infoVer = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                var nameVer = assembly.GetName().Version?.ToString();
                var version = infoVer ?? nameVer ?? "1.0.0";
                VersionTextBlock.Text = version;
            }
            catch
            {
                VersionTextBlock.Text = "1.0.0";
            }

            // Update current user display
            UpdateCurrentUserDisplay();

            // Update button visibility based on user role
            UpdateButtonVisibility();

            // Load default Home view
            MainContent.Content = new HomeView();
        }

        private void UpdateCurrentUserDisplay()
        {
            var user = _userManager?.UserInfo;
            if (user != null)
            {
                CurrentUserNameText.Text = $"帳號: {user.Name}";
                CurrentUserRoleText.Text = $"權限: {user.CurrentUserRole}";
            }
            else
            {
                CurrentUserNameText.Text = "帳號: -";
                CurrentUserRoleText.Text = "權限: -";
            }
        }

        /// <summary>
        /// 根據目前登入使用者的權限更新按鈕可見性
        /// - Developer 按鈕：僅 Developer 可見
        /// - User 按鈕：Administrator 與 Developer 可見
        /// - Setting 按鈕：Administrator、Developer、Engineer 可見
        /// 未登入或其他則隱藏
        /// </summary>
        private void UpdateButtonVisibility()
        {
            if (BtnDeveloper != null) BtnDeveloper.Visibility = Visibility.Collapsed;
            if (BtnUser != null) BtnUser.Visibility = Visibility.Collapsed;
            if (BtnSetting != null) BtnSetting.Visibility = Visibility.Collapsed;

            var role = _userManager?.UserInfo?.CurrentUserRole;
            if (role == null) return;

            switch (role)
            {
                case UserRole.Developer:
                    if (BtnDeveloper != null) BtnDeveloper.Visibility = Visibility.Visible;
                    if (BtnUser != null) BtnUser.Visibility = Visibility.Visible;
                    if (BtnSetting != null) BtnSetting.Visibility = Visibility.Visible;
                    break;

                case UserRole.Administrator:
                    if (BtnUser != null) BtnUser.Visibility = Visibility.Visible;
                    if (BtnSetting != null) BtnSetting.Visibility = Visibility.Visible;
                    break;

                case UserRole.Engineer:
                    if (BtnSetting != null) BtnSetting.Visibility = Visibility.Visible;
                    break;

                case UserRole.Operator:
                default:
                    // remain collapsed
                    break;
            }
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new HomeView();
        }

        private void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ManualView();
        }

        private void BtnIO_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new IOView();
        }

        private void BtnAlarm_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new AlarmView();
        }

        private void BtnInfo_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new InfoView();
        }

        private void BtnSetting_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new SettingView();
        }

        private void BtnUser_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new UserView();
        }

        private void BtnDeveloper_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new DeveloperView();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide main window while showing login
            this.Hide();
            var loginWindow = _services.GetRequiredService<LoginWindow>();
            bool? result = loginWindow.ShowDialog();
            if (result == true)
            {
                // Logged in again, update display and show main
                UpdateCurrentUserDisplay();
                UpdateButtonVisibility();
                // Return to Home page after re-login
                MainContent.Content = new HomeView();
                this.Show();
            }
            else
            {
                // User cancelled or failed login -> exit app
                Application.Current.Shutdown();
            }
        }
    }
}