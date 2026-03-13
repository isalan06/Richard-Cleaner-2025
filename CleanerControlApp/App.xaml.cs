using CleanerControlApp.Hardwares.DryingTank.Interfaces;
using CleanerControlApp.Hardwares.DryingTank.Services;
using CleanerControlApp.Modules.DeltaMS300.Interfaces;
using CleanerControlApp.Modules.DeltaMS300.Services;
using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using CleanerControlApp.Modules.MitsubishiPLC.Services;
using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.Modbus.Services;
using CleanerControlApp.Modules.TempatureController.Interfaces;
using CleanerControlApp.Modules.TempatureController.Services;
using CleanerControlApp.Modules.UltrasonicDevice.Interfaces;
using CleanerControlApp.Modules.UltrasonicDevice.Services;
using CleanerControlApp.Modules.UserManagement.Services;
using CleanerControlApp.Utilities;
using CleanerControlApp.Utilities.HandShaking;
using CleanerControlApp.Vision;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using SQLitePCL;
using System.Configuration;
using System.Data;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using CleanerControlApp.Services;
using CleanerControlApp.Hardwares.Sink.Interfaces;
using CleanerControlApp.Hardwares.Sink.Services;
using CleanerControlApp.Hardwares.HeatingTank.Interfaces;
using CleanerControlApp.Hardwares.HeatingTank.Services;
using CleanerControlApp.Hardwares;
using CleanerControlApp.Hardwares.SoakingTank.Interfaces;
using CleanerControlApp.Hardwares.SoakingTank.Services;
using CleanerControlApp.Modules.Motor.Services;
using CleanerControlApp.Modules.Motor.Interfaces;
using CleanerControlApp.Hardwares.Shuttle.Interfaces;
using CleanerControlApp.Hardwares.Shuttle.Services;

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
            // Register global exception handlers early
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            try
            {
                Batteries_V2.Init(); // 初始化 SQLitePCL

                //先讀取 appsettings.json，決定是否顯示 Console 視窗，不然無法在 Logger 設定前顯示
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                var config = configBuilder.Build();
                bool showConsole = config.GetSection("AppSettings").GetValue<bool>("EnableConsoleWindow");
                if (showConsole)
                {
                    ShowConsole();
                }

                //先初始化使用者資料庫
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
                        //這裡的 hostContext.Configuration 已經包含了 appsettings.json 的設定，可以直接使用
                        var configuration = hostContext.Configuration;

                        ConfigLoader.Load();

                        // 預先載入設定並綁定到 AppSettings 類別，確保在註冊服務前就有設定物件可用
                        var settings = new AppSettings();
                        settings = ConfigLoader.GetSettings();

                        // 同樣載入通訊設定，確保在註冊服務前就有設定物件可用
                        var communicationSettings = new CommunicationSettings();
                        communicationSettings = ConfigLoader.GetCommunicationSettings();

                        // 同樣載入元件設定，確保在註冊服務前就有設定物件可用
                        var unitSettings = new UnitSettings();
                        unitSettings = ConfigLoader.GetUnitSettings();

                        // 同樣載入元件設定，確保在註冊服務前就有設定物件可用
                        var moduleSettings = new ModuleSettings();
                        moduleSettings = ConfigLoader.GetModuleSettings();

                        // 註冊設定物件
                        services.Configure<AppSettings>(hostContext.Configuration.GetSection("AppSettings"));
                        services.AddSingleton(settings); //直接註冊已經綁定的設定物件，讓它可以被注入到需要的地方
                        services.Configure<CommunicationSettings>(hostContext.Configuration.GetSection("CommunicationSettings"));
                        services.AddSingleton(communicationSettings); //直接註冊已經綁定的通訊設定物件，讓它可以被注入到需要的地方
                        services.Configure<UnitSettings>(hostContext.Configuration.GetSection("UnitSettings"));
                        services.AddSingleton(unitSettings);
                        services.Configure<ModuleSettings>(hostContext.Configuration.GetSection("ModuleSettings"));
                        services.AddSingleton(moduleSettings);

                        services.AddSingleton<HandShakingManager>(); // 註冊 HandShakingManager 為 Singleton

                        services.AddSingleton<UserManager>(); // 註冊 UserManager 為 Singleton
                        services.AddTransient<LoginWindow>(); // 改為 Transient
                        services.AddSingleton<MainWindow>(); // 註冊 MainWindow 為 Singleton
                        services.AddSingleton<IModbusTCPService>(sp =>
                        {
                            // Read configuration values
                            var cfgSection = hostContext.Configuration.GetSection("CommunicationSettings:ModbusTCPParameter");
                            var ip = (communicationSettings.ModbusTCPParameter == null) ? "127.0.0.1" : communicationSettings.ModbusTCPParameter.IP; //直接從已綁定的設定物件讀取
                            var port = (communicationSettings.ModbusTCPParameter == null) ?502 : communicationSettings.ModbusTCPParameter.Port; //直接從已綁定的設定物件讀取

                            // Resolve logger (ILogger<T> is provided by the host)
                            var logger = sp.GetService<ILogger<ModbusTCPService>>();

                            // Create instance and apply settings
                            ModbusTCPService svc;
                            if (logger != null)
                                svc = new ModbusTCPService(logger);
                            else
                                svc = new ModbusTCPService();

                            if (!string.IsNullOrWhiteSpace(ip))
                                svc.Ip = ip!;
                            if (port >0)
                                svc.Port = port;

                            return svc as IModbusTCPService;
                        });
                        services.AddSingleton<IPLCService, PLCService>();
                        // Also register IPLCOperator to resolve to the same PLCService singleton
                        services.AddSingleton<IPLCOperator>(sp => (IPLCOperator)sp.GetRequiredService<IPLCService>());

                        // Register ModbusRTUService using factory and apply serial settings from configuration
                        services.AddSingleton<IModbusRTUService>(sp =>
                        {
                            //var cfg = hostContext.Configuration.GetSection("CommunicationSettings:ModbusRTUParameter");
                            var cfg = communicationSettings.ModbusRTUParameter; //直接從已綁定的設定物件讀取
                            var portName = string.IsNullOrWhiteSpace(cfg?.PortName) ? "COM1" : cfg!.PortName;
                            var baud = (cfg?.BaudRate ??0) >0 ? cfg!.BaudRate :9600;
                            var parityStr = string.IsNullOrWhiteSpace(cfg?.Parity) ? "None" : cfg!.Parity;
                            var dataBits = (cfg?.DataBits ??0) >0 ? cfg!.DataBits :8;
                            var stopBitVal = (cfg?.StopBits ??0) >0 ? cfg!.StopBits :1;

                            var logger = sp.GetService<ILogger<ModbusRTUService>>();

                            ModbusRTUService svc;
                            if (logger != null)
                                svc = new ModbusRTUService(logger);
                            else
                                svc = new ModbusRTUService();

                            if (!string.IsNullOrWhiteSpace(portName))
                                svc.PortName = portName!;

                            if (baud >0)
                                svc.BaudRate = baud;

                            if (dataBits >0)
                                svc.DataBits = dataBits;

                            // parse parity
                            if (!string.IsNullOrWhiteSpace(parityStr))
                            {
                                if (Enum.TryParse<System.IO.Ports.Parity>(parityStr, true, out var parity))
                                {
                                    svc.Parity = parity;
                                }
                                else
                                {
                                    // fallback mapping for common names
                                    switch (parityStr.Trim().ToLowerInvariant())
                                    {
                                        case "none": svc.Parity = System.IO.Ports.Parity.None; break;
                                        case "odd": svc.Parity = System.IO.Ports.Parity.Odd; break;
                                        case "even": svc.Parity = System.IO.Ports.Parity.Even; break;
                                        case "mark": svc.Parity = System.IO.Ports.Parity.Mark; break;
                                        case "space": svc.Parity = System.IO.Ports.Parity.Space; break;
                                    }
                                }
                            }

                            // map stop bit value to StopBits enum
                            switch (stopBitVal)
                            {
                                case 0: svc.StopBits = StopBits.None; break;
                                case 1: svc.StopBits = StopBits.One; break;
                                case 2: svc.StopBits = StopBits.Two; break;
                                case 3: svc.StopBits = StopBits.OnePointFive; break;
                                default: svc.StopBits = StopBits.One; break;
                            }

                            return svc as IModbusRTUService;
                        });

                        // ILogger<ModbusRTUPoolService> will be resolved from DI; provide the int explicitly
                        // Construct pool service using configured pool parameters (if any)
                        services.AddSingleton<IModbusRTUPollService>(sp =>
                            new ModbusRTUPoolService(sp.GetRequiredService<ILogger<ModbusRTUPoolService>>(), communicationSettings.ModbusRTUPoolParameter));

                        // Register DeltaMS300 instances (2 modules) and expose as arrays for injection
                        services.AddSingleton<DeltaMS300[]>(sp =>
                        {
                            var pool = sp.GetRequiredService<IModbusRTUPollService>();
                            var loggerFactory = sp.GetService<ILoggerFactory>();
                            var arr = new DeltaMS300[DeltaMS300.ModuleCount];
                            for (int i =0; i < DeltaMS300.ModuleCount; i++)
                            {
                                var logger = loggerFactory?.CreateLogger<DeltaMS300>();
                                arr[i] = new DeltaMS300(i, pool, logger);
                            }
                            return arr;
                        });

                        // Also register as IDeltaMS300[] so consumers can request the interface array
                        services.AddSingleton<IDeltaMS300[]>(sp => (IDeltaMS300[])sp.GetRequiredService<DeltaMS300[]>());

                        // Register SingleAxisMotor instances (2 modules) and expose as arrays for injection
                        services.AddSingleton<SingleAxisMotor[]>(sp =>
                        {
                            var loggerFactory = sp.GetService<ILoggerFactory>();
                            var plc = sp.GetRequiredService<IPLCOperator>();
                            var unitSettingsLocal = sp.GetRequiredService<UnitSettings>();
                            var moduleSettingsLocal = sp.GetRequiredService<ModuleSettings>();

                            var arr = new SingleAxisMotor[2];
                            for (int i =0; i < arr.Length; i++)
                            {
                                var logger = loggerFactory?.CreateLogger<SingleAxisMotor>();
                                arr[i] = new SingleAxisMotor(i, logger, unitSettingsLocal, moduleSettingsLocal, plc);
                            }

                            return arr;
                        });

                        // Also register as ISingleAxisMotor[] so consumers can request the interface array
                        services.AddSingleton<ISingleAxisMotor[]>(sp => sp.GetRequiredService<SingleAxisMotor[]>().Cast<ISingleAxisMotor>().ToArray());

                        // Register TemperatureControllers using a factory so dependencies are resolved explicitly
                        services.AddSingleton<ITemperatureControllers>(sp =>
                        new TemperatureControllers(
                        sp.GetRequiredService<IModbusRTUPollService>(),
                        sp.GetRequiredService<ILogger<TemperatureControllers>>()
                        ));
                        // Register UltrasonicDevice using a factory so dependencies are resolved explicitly
                        services.AddSingleton<IUltrasonicDevice>(sp =>
                        new UltrasonicDevice(
                        sp.GetRequiredService<IModbusRTUPollService>(),
                        sp.GetRequiredService<ILogger<UltrasonicDevice>>()
                        ));

                        services.AddSingleton<IDryingTank[]>(sp =>
                        {
                            var loggerFactory = sp.GetService<ILoggerFactory>();
                            var plc = sp.GetRequiredService<IPLCOperator>();
                            var tempControllers = sp.GetRequiredService<ITemperatureControllers>();
                            var unitSettings = sp.GetRequiredService<UnitSettings>();
                            var moduleSetting = sp.GetRequiredService<ModuleSettings>();

                            var arr = new IDryingTank[DryingTank.DryingTankCount];
                            for (int i =0; i < DryingTank.DryingTankCount; i++)
                            {
                                var logger = loggerFactory?.CreateLogger<DryingTank>();
                                arr[i] = new DryingTank(i, logger, plc, tempControllers, unitSettings, moduleSetting);
                            }

                            return arr;
                        });


                        // Register Sink and ISink; resolve DeltaMS300[0] as driver dependency
                        services.AddSingleton<Sink>(sp =>
                        {
                            var logger = sp.GetService<ILogger<Sink>>();
                            var plc = sp.GetRequiredService<IPLCOperator>();
                            var tempControllers = sp.GetService<ITemperatureControllers>();
                            var unitSettings = sp.GetRequiredService<UnitSettings>();
                            var moduleSettings = sp.GetRequiredService<ModuleSettings>();
                            var deltaArr = sp.GetRequiredService<IDeltaMS300[]>();
                            var delta = (deltaArr != null && deltaArr.Length >0) ? deltaArr[Sink.MS300_Index] : null!; // expect at least one

                            return new Sink(logger, plc, tempControllers, unitSettings, moduleSettings, delta);
                        });
                        // Also register concrete Sink type for consumers that request it
                        services.AddSingleton<ISink>(sp => (ISink)sp.GetRequiredService<Sink>());

                        // Register HeatingTank and IHeatingTank (resolve DeltaMS300 instance by index)
                        services.AddSingleton<HeatingTank>(sp =>
                        {
                            var logger = sp.GetService<ILogger<HeatingTank>>();
                            var plc = sp.GetRequiredService<IPLCOperator>();
                            var tempControllers = sp.GetRequiredService<ITemperatureControllers>();
                            var unitSettings = sp.GetRequiredService<UnitSettings>();
                            var moduleSettings = sp.GetRequiredService<ModuleSettings>();
                            var deltaArr = sp.GetRequiredService<IDeltaMS300[]>();
                            var delta = (deltaArr != null && deltaArr.Length > HeatingTank.MS300_Index) ? deltaArr[HeatingTank.MS300_Index] : null!;

                            return new HeatingTank(logger, plc, tempControllers, unitSettings, moduleSettings, delta);
                        });
                        
                        // Expose interface mapping to the same singleton
                        services.AddSingleton<IHeatingTank>(sp => (IHeatingTank)sp.GetRequiredService<HeatingTank>());

                        // Register SoakingTank and ISoakingTank (singleton)
                        services.AddSingleton<SoakingTank>(sp =>
                        {
                            var logger = sp.GetService<ILogger<SoakingTank>>();
                            var plc = sp.GetRequiredService<IPLCOperator>();
                            var unitSettings = sp.GetRequiredService<UnitSettings>();
                            var moduleSettings = sp.GetRequiredService<ModuleSettings>();
                            // heating tank is optional for soaking tank; resolve if available
                            var heatingTank = sp.GetService<IHeatingTank>();
                            var ultrasonic = sp.GetRequiredService<IUltrasonicDevice>();

                            return new SoakingTank(logger, plc, unitSettings, moduleSettings, heatingTank, ultrasonic);
                        });
                        services.AddSingleton<ISoakingTank>(sp => (ISoakingTank)sp.GetRequiredService<SoakingTank>());



                        // Register Shuttle and IShuttle
                        services.AddSingleton<Shuttle>(sp =>
                        {
                            var logger = sp.GetService<ILogger<Shuttle>>();
                            var unitSettingsLocal = sp.GetRequiredService<UnitSettings>();
                            var moduleSettingsLocal = sp.GetRequiredService<ModuleSettings>();
                            var plc = sp.GetRequiredService<IPLCOperator>();
                            var motors = sp.GetRequiredService<ISingleAxisMotor[]>();
                            ISingleAxisMotor? motorX = (motors != null && motors.Length >0) ? motors[0] : null;
                            ISingleAxisMotor? motorZ = (motors != null && motors.Length >1) ? motors[1] : null;

                            return new Shuttle(logger, unitSettingsLocal, moduleSettingsLocal, plc, motorX, motorZ);
                        });
                        services.AddSingleton<IShuttle>(sp => (IShuttle)sp.GetRequiredService<Shuttle>());

                        


                        // Register HardwareManager as singleton and resolve its dependencies from the container
                        services.AddSingleton<HardwareManager>(sp =>
                        {
                            var logger = sp.GetRequiredService<ILogger<HardwareManager>>();
                            var unitSettingsLocal = sp.GetRequiredService<UnitSettings>();
                            var moduleSettingsLocal = sp.GetRequiredService<ModuleSettings>();

                            // optional hardware components
                            var sink = sp.GetService<ISink>();
                            var soaking = sp.GetService<ISoakingTank>();
                            var drying = sp.GetService<IDryingTank[]>();
                            var shuttle = sp.GetService<IShuttle>();
                            var heating = sp.GetService<IHeatingTank>();

                            // communication services (some may be optional)
                            var modbusTcp = sp.GetService<IModbusTCPService>();
                            var modbusRtuPoll = sp.GetService<IModbusRTUPollService>();

                            // required components
                            var delta = sp.GetRequiredService<IDeltaMS300[]>();
                            var plcService = sp.GetRequiredService<IPLCService>();
                            var plcOperator = sp.GetRequiredService<IPLCOperator>(); 
                            var tempControllers = sp.GetRequiredService<ITemperatureControllers>();
                            var ultrasonic = sp.GetRequiredService<IUltrasonicDevice>();

                            return new HardwareManager(logger, unitSettingsLocal, moduleSettingsLocal,
                            sink, soaking, drying, shuttle, heating,
                            modbusTcp, modbusRtuPoll,
                            delta, plcService, plcOperator, tempControllers, ultrasonic);
                        });
                        
                        // Register System background service to run with the host
                        services.AddHostedService<SystemBackgroundService>();

                    })
                    .Build();
            }
            catch (Exception ex)
            {
                // Ensure any failure during construction is reported and not swallowed by runtime
                try
                {
                    var msg = $"Application initialization failed: {ex.GetType().FullName}: {ex.Message}";
                    Console.WriteLine(msg);
                    MessageBox.Show(msg, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }

                // Re-throw to allow debugging if desired
                throw;
            }
        }

        private void App_DispatcherUnhandledException(object? sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                var msg = $"Unhandled UI exception: {e.Exception.GetType().FullName}: {e.Exception.Message}";
                Console.WriteLine(msg);
                MessageBox.Show(msg, "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch { }
        }

        private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                var msg = ex != null ? $"Unhandled domain exception: {ex.GetType().FullName}: {ex.Message}" : "Unhandled domain exception (non-Exception)";
                Console.WriteLine(msg);
                MessageBox.Show(msg, "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch { }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                var msg = $"Unobserved task exception: {e.Exception.GetType().FullName}: {e.Exception.Message}";
                Console.WriteLine(msg);
                MessageBox.Show(msg, "Task Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                e.SetObserved();
            }
            catch { }
        }

        /// <summary>
        /// 啟動應用程式
        /// </summary>
        /// <param name="e"></param>
        protected override async void OnStartup(StartupEventArgs e)
        {
            if (AppHost == null)
            {
                MessageBox.Show("Application host failed to initialize.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            try
            {
                await AppHost.StartAsync();
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
            }
            catch (Exception ex)
            {
                try
                {
                    var msg = $"Host start failed: {ex.GetType().FullName}: {ex.Message}";
                    Console.WriteLine(msg);
                    MessageBox.Show(msg, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }

                Shutdown();
            }

            base.OnStartup(e);
        }

        /// <summary>
        /// 關閉應用程式
        /// </summary>
        /// <param name="e"></param>
        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                if (AppHost != null)
                {
                    await AppHost.StopAsync();
                }
            }
            catch { }
            base.OnExit(e);
        }

    }

}
