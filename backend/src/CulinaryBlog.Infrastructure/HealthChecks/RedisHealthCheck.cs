using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure.HealthChecks;

/// <summary>
/// Health check kiểm tra khả năng kết nối và độ trễ thực tế tới Redis server.
/// Không lộ thông tin xác thực hay cấu hình nhạy cảm.
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private static IConnectionMultiplexer? _multiplexer;
    private static readonly object LockObj = new();

    public RedisHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var redisConn = _configuration.GetConnectionString("Redis")
            ?? _configuration["REDIS_CONNECTION"];

        if (string.IsNullOrWhiteSpace(redisConn))
        {
            return HealthCheckResult.Unhealthy("Redis connection string is not configured.");
        }

        try
        {
            EnsureConnected(redisConn);

            if (_multiplexer == null || !_multiplexer.IsConnected)
            {
                return HealthCheckResult.Unhealthy("Redis server is disconnected.");
            }

            var db = _multiplexer.GetDatabase();
            var latency = await db.PingAsync();

            return HealthCheckResult.Healthy($"Redis is operational. Latency: {latency.TotalMilliseconds:F1}ms.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Redis check failed: {ex.Message}");
        }
    }

    private static void EnsureConnected(string connectionString)
    {
        if (_multiplexer == null || !_multiplexer.IsConnected)
        {
            lock (LockObj)
            {
                if (_multiplexer == null || !_multiplexer.IsConnected)
                {
                    var options = ConfigurationOptions.Parse(connectionString);
                    options.ConnectTimeout = 3000;
                    options.SyncTimeout = 3000;
                    options.AbortOnConnectFail = false;
                    _multiplexer = ConnectionMultiplexer.Connect(options);
                }
            }
        }
    }
}
