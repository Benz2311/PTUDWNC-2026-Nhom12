using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.Infrastructure.HealthChecks;

/// <summary>
/// Health check kiểm tra khả năng kết nối thực tế tới PostgreSQL Database.
/// Không lộ chuỗi kết nối, username hoặc password ra ngoài.
/// </summary>
public class PostgresHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext? _dbContext;
    private readonly Func<CancellationToken, Task<bool>>? _connectionTester;

    public PostgresHealthCheck(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Constructor hỗ trợ Unit Test độc lập không phụ thuộc kết nối vật lý.
    /// </summary>
    public PostgresHealthCheck(Func<CancellationToken, Task<bool>> connectionTester)
    {
        _connectionTester = connectionTester;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = _connectionTester != null
                ? await _connectionTester(cancellationToken)
                : await _dbContext!.Database.CanConnectAsync(cancellationToken);

            if (canConnect)
            {
                return HealthCheckResult.Healthy("PostgreSQL database connection is operational.");
            }

            return HealthCheckResult.Unhealthy("Cannot connect to PostgreSQL database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"PostgreSQL check failed: {ex.Message}");
        }
    }
}
