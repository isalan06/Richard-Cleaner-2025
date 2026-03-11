using CleanerControlApp.Hardwares;
using CleanerControlApp.Utilities.Alarm;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanerControlApp.Services
{
    /// <summary>
    /// Background service scaffold where you can put initialization, polling and shutdown code.
    /// Modify the virtual methods below to add your logic.
    /// </summary>
    public class SystemBackgroundService : BackgroundService
    {
        private readonly ILogger<SystemBackgroundService> _logger;
        private readonly IServiceProvider _services;
        private readonly HardwareManager _hardwareManager;

        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

        public SystemBackgroundService(ILogger<SystemBackgroundService> logger, IServiceProvider services, HardwareManager hardwareManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _hardwareManager = hardwareManager ?? throw new ArgumentNullException(nameof(hardwareManager));
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SystemBackgroundService starting...");
            try
            {
                await InitializeAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SystemBackgroundService initialization.");
                throw;
            }

            await base.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Called once when the background service starts. Put initialization code here.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task InitializeAsync(CancellationToken cancellationToken)
        {
            // TODO: add initialization logic here
            _logger.LogDebug("InitializeAsync - put startup code here.");

            // Use asynchronous HardwareManager methods to avoid blocking the startup thread
            try
            {
                // Fire-and-forget: kick off connect but do not await here so startup proceeds immediately.
                Task.Run(async () =>
                {
                    try
                    {
                        await _hardwareManager.CommunicationConnectAsync(true).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception thrown by HardwareManager.CommunicationConnectAsync(true) (fire-and-forget). Continue startup.");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown by HardwareManager.CommunicationConnectAsync(true). Continue startup.");
            }

            try
            {
                await _hardwareManager.ModuleRunningAsync(true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown by HardwareManager.ModuleRunningAsync(true). Continue startup.");
            }

            return;
        }

        /// <summary>
        /// Main loop. This runs until the service is stopped. Put periodic polling logic in PollOnceAsync.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SystemBackgroundService is running.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await PollOnceAsync(stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        // shutdown requested
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled exception inside PollOnceAsync. Continue loop.");
                    }

                    try
                    {
                        await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                _logger.LogInformation("SystemBackgroundService main loop exiting.");
            }
        }

        /// <summary>
        /// One polling iteration. Override or extend to perform periodic work.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task PollOnceAsync(CancellationToken cancellationToken)
        {
            // TODO: add polling logic here
            //_logger.LogDebug("PollOnceAsync - put periodic work here.");

            // Ensure AlarmManager polls registered flag getters so changes 
            // are detected and logged.
            AlarmManager.CheckFlagGetters();

            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SystemBackgroundService stopping...");
            try
            {
                await OnStoppedAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SystemBackgroundService shutdown.");
            }

            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Called when the service is stopping. Put cleanup logic here.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task OnStoppedAsync(CancellationToken cancellationToken)
        {
            // TODO: add cleanup logic here
            _logger.LogDebug("OnStoppedAsync - put cleanup code here.");
            return Task.CompletedTask;
        }
    }
}
