using CleanerControlApp.Modules.UserManagement.Services;
using CleanerControlApp.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;

using SQLitePCL;
using CleanerControlApp.Vision;
using NLog.Extensions.Logging;
using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.Modbus.Services;

namespace CleanerControlApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        #region Console 控制台視窗切換  
        
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();

        public static void ShowConsole()
        {
            AllocConsole();
            //Console.WriteLine("Test Console Output");
        }

        public static void HideConsole()
        {
            FreeConsole();
        }

        #endregion

        public static IHost? AppHost { get; private set; } = null;

        public App()
        {
            Batteries_V2.Init(); // 初始化 SQLitePCL

            // 先讀取 appsettings.json，決定是否顯示 Console 視窗，不然無法在 Logger 設定前顯示
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var config = configBuilder.Build();
            bool showConsole = config.GetSection("AppSettings").GetValue<bool>("EnableConsoleWindow");
            if (showConsole)
            {
                ShowConsole();
            }

            // 先初始化使用者資料庫
            UserRepository.Initialize();

            // 建立應用程式主機
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    // 根據設定決定是否啟用 Console Logger
                    var enableConsole = context.Configuration.GetSection("AppSettings").GetValue<bool>("EnableConsole");
                    if (enableConsole)
                    {
                        logging.AddConsole(); // 加入 Console Logger
                        logging.AddDebug(); // 加入 Debug Logger
                    }
                    // logging.AddProvider(new 第三方LoggerProvider()); // 日後擴充
                    logging.AddNLog(); // 加入 NLog
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // 註冊設定物件
                    services.Configure<AppSettings>(hostContext.Configuration.GetSection("AppSettings"));
                    services.AddSingleton<UserManager>(); // 註冊 UserManager 為 Singleton
                    services.AddTransient<LoginWindow>(); // 改為 Transient
                    services.AddSingleton<MainWindow>(); // 註冊 MainWindow 為 Singleton
                    services.AddSingleton<IModbusTCPService, ModbusTCPService>(); // 註冊 ModbusTCPService 為 Singleton
                })
                .Build();
        }

        /// <summary>
        /// 啟動應用程式
        /// </summary>
        /// <param name="e"></param>
        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost!.StartAsync();
            var loginWindow = AppHost.Services.GetRequiredService<LoginWindow>();
            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            bool? loginResult = loginWindow.ShowDialog();
            if (loginResult == true)
            {
                mainWindow.Show();
            }
            else
            {
                Shutdown(); // 登入失敗或取消則關閉程式
            }
            base.OnStartup(e);
        }

        /// <summary>
        /// 關閉應用程式
        /// </summary>
        /// <param name="e"></param>
        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost!.StopAsync();
            base.OnExit(e);
        }

    }

}
