using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;
using Minio.DataModel.Args;
using Npgsql;
using System.Diagnostics;
using Telegram.Bot;

namespace GameStore.Services.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(IConfiguration configuration, ILogger<DatabaseHealthCheck> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));

                await connection.OpenAsync(cancellationToken);

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);

                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > 3000)
                {
                    return HealthCheckResult.Degraded(
                        $"Database response time is slow: {stopwatch.ElapsedMilliseconds}ms",
                        data: new Dictionary<string, object>
                        {
                            ["ResponseTime"] = stopwatch.ElapsedMilliseconds
                        });
                }

                return HealthCheckResult.Healthy(
                    $"Database is responsive: {stopwatch.ElapsedMilliseconds}ms",
                    data: new Dictionary<string, object>
                    {
                        ["ResponseTime"] = stopwatch.ElapsedMilliseconds
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy(
                    "Database connection failed",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        ["Error"] = ex.Message
                    });
            }
        }
    }

    public class MinioHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MinioHealthCheck> _logger;

        public MinioHealthCheck(IConfiguration configuration, ILogger<MinioHealthCheck> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoint = _configuration["MinIO:Endpoint"] ?? "localhost:9000";
                var accessKey = _configuration["MinIO:AccessKey"] ?? "minioadmin";
                var secretKey = _configuration["MinIO:SecretKey"] ?? "minioadmin";
                var useSSL = bool.Parse(_configuration["MinIO:UseSSL"] ?? "false");

                var minioClient = new MinioClient()
                    .WithEndpoint(endpoint)
                    .WithCredentials(accessKey, secretKey)
                    .WithSSL(useSSL)
                    .Build();

                var stopwatch = Stopwatch.StartNew();

                // List buckets as a health check
                var buckets = await minioClient.ListBucketsAsync(cancellationToken);

                stopwatch.Stop();

                return HealthCheckResult.Healthy(
                    $"MinIO is accessible: {buckets.Buckets.Count} buckets found, response time: {stopwatch.ElapsedMilliseconds}ms",
                    data: new Dictionary<string, object>
                    {
                        ["BucketCount"] = buckets.Buckets.Count,
                        ["ResponseTime"] = stopwatch.ElapsedMilliseconds
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MinIO health check failed");
                return HealthCheckResult.Unhealthy(
                    "MinIO connection failed",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        ["Error"] = ex.Message
                    });
            }
        }
    }

    public class TelegramHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TelegramHealthCheck> _logger;

        public TelegramHealthCheck(IConfiguration configuration, ILogger<TelegramHealthCheck> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var botToken = _configuration["TelegramBot:Token"];
                if (string.IsNullOrEmpty(botToken))
                {
                    return HealthCheckResult.Unhealthy("Telegram bot token not configured");
                }

                var botClient = new Telegram.Bot.TelegramBotClient(botToken);
                var me = await botClient.GetMe(cancellationToken);

                return HealthCheckResult.Healthy(
                    $"Telegram bot is accessible: @{me.Username}",
                    data: new Dictionary<string, object>
                    {
                        ["BotUsername"] = me.Username ?? "Unknown",
                        ["BotId"] = me.Id
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram health check failed");
                return HealthCheckResult.Degraded(
                    "Telegram bot connection failed - notifications may not work",
                    exception: ex);
            }
        }
    }
}