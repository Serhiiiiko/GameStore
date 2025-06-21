using GameStore.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;

namespace GameStore.Services
{
    public class HealthMonitoringService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HealthMonitoringService> _logger;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _criticalCheckInterval = TimeSpan.FromSeconds(30);
        private DateTime _lastNotificationSent = DateTime.MinValue;
        private readonly TimeSpan _notificationCooldown = TimeSpan.FromMinutes(5);

        public HealthMonitoringService(
            IServiceProvider serviceProvider,
            ILogger<HealthMonitoringService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Initial delay

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();
                    var telegramService = scope.ServiceProvider.GetService<ITelegramNotificationService>();

                    var healthReport = await healthCheckService.CheckHealthAsync(stoppingToken);

                    if (healthReport.Status != HealthStatus.Healthy)
                    {
                        _logger.LogWarning($"Health check status: {healthReport.Status}");

                        // Check if we should send notification (cooldown period)
                        if (DateTime.UtcNow - _lastNotificationSent > _notificationCooldown)
                        {
                            await SendHealthNotificationAsync(healthReport, telegramService);
                            _lastNotificationSent = DateTime.UtcNow;
                        }

                        // If critical, check more frequently
                        if (healthReport.Status == HealthStatus.Unhealthy)
                        {
                            await Task.Delay(_criticalCheckInterval, stoppingToken);
                            continue;
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Health check passed");
                    }

                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in health monitoring service");
                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
        }

        private async Task SendHealthNotificationAsync(HealthReport report, ITelegramNotificationService telegramService)
        {
            if (telegramService == null) return;

            var message = new StringBuilder();
            message.AppendLine($"🚨 Health Check Alert!");
            message.AppendLine($"\nStatus: {report.Status}");
            message.AppendLine($"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

            foreach (var entry in report.Entries)
            {
                message.AppendLine($"\n{entry.Key}: {entry.Value.Status}");
                if (entry.Value.Exception != null)
                {
                    message.AppendLine($"Error: {entry.Value.Exception.Message}");
                }
                if (entry.Value.Description != null)
                {
                    message.AppendLine($"Description: {entry.Value.Description}");
                }
            }

            message.AppendLine($"\nEnvironment: {_configuration["ASPNETCORE_ENVIRONMENT"]}");
            message.AppendLine($"Server: {Environment.MachineName}");

            try
            {
                await telegramService.SendHealthAlertAsync(message.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram notification");
            }
        }
    }
}