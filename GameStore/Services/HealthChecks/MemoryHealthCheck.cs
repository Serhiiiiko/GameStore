using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace GameStore.Services.HealthChecks
{
    public class MemoryHealthCheck : IHealthCheck
    {
        private readonly long _maxMemoryMB;
        private readonly ILogger<MemoryHealthCheck> _logger;

        public MemoryHealthCheck(IConfiguration configuration, ILogger<MemoryHealthCheck> logger)
        {
            _maxMemoryMB = configuration.GetValue<long>("HealthCheck:MaxMemoryMB", 500);
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                var memoryMB = process.WorkingSet64 / (1024 * 1024);

                var data = new Dictionary<string, object>
                {
                    ["MemoryMB"] = memoryMB,
                    ["MaxMemoryMB"] = _maxMemoryMB
                };

                if (memoryMB > _maxMemoryMB)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Memory usage is high: {memoryMB}MB (max: {_maxMemoryMB}MB)",
                        data: data));
                }

                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Memory usage: {memoryMB}MB",
                    data: data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Memory health check failed");
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Memory health check failed",
                    exception: ex));
            }
        }
    }
}