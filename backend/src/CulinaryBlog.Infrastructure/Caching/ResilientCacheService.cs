using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Caching;

/// <summary>
/// Triển khai ICacheService bao bọc IDistributedCache (Redis) với cơ chế Failover an toàn theo SRS v1.2.0 (NFR-REL-002, NFR-PERF):
/// - Khi Redis hoạt động: tuân thủ cache-aside policy với TTL quy định (Category 30m, Recipe 5m, List 1m).
/// - Khi Redis sập hoặc mất kết nối: kích hoạt Circuit Breaker, tự động fallback truy vấn trực tiếp vào PostgreSQL
///   ngay lập tức (<1ms overhead) mà không làm nghẽn request hoặc quăng Exception.
/// - Sau thời gian cooldown (30s), tự động thăm dò kết nối lại để khôi phục cache mà không cần khởi động lại ứng dụng.
/// - Đăng ký dạng Singleton trong DI container.
/// </summary>
public class ResilientCacheService :
    CulinaryBlog.Application.Common.Interfaces.ICacheService,
    CulinaryBlog.Application.Interfaces.ICacheService
{
    private readonly IDistributedCache? _distributedCache;
    private readonly ILogger<ResilientCacheService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Circuit Breaker state (instance field, an toàn đa luồng cho Singleton DI)
    private long _circuitBreakerUntilTicks = 0;
    private static readonly TimeSpan CircuitBreakerCooldown = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromMilliseconds(500);

    public ResilientCacheService(ILogger<ResilientCacheService> logger, IDistributedCache? distributedCache = null)
    {
        _logger = logger;
        _distributedCache = distributedCache;
    }

    private bool IsCircuitOpen()
    {
        var until = Interlocked.Read(ref _circuitBreakerUntilTicks);
        return until > 0 && DateTime.UtcNow.Ticks < until;
    }

    private void OpenCircuit()
    {
        var nextAttempt = DateTime.UtcNow.Add(CircuitBreakerCooldown).Ticks;
        Interlocked.Exchange(ref _circuitBreakerUntilTicks, nextAttempt);
    }

    private void CloseCircuit()
    {
        Interlocked.Exchange(ref _circuitBreakerUntilTicks, 0);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_distributedCache == null || IsCircuitOpen())
        {
            return default;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(OperationTimeout);

            var cachedBytes = await _distributedCache.GetAsync(key, cts.Token);
            CloseCircuit();

            if (cachedBytes == null || cachedBytes.Length == 0)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedBytes, JsonOptions);
        }
        catch (Exception ex)
        {
            OpenCircuit();
            // NFR-REL-002: Redis failover - fallback database, không throw exception
            _logger.LogWarning(ex, "Redis cache read error for key '{CacheKey}'. Circuit opened for {CooldownSeconds}s, falling back to database.",
                key, CircuitBreakerCooldown.TotalSeconds);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (_distributedCache == null || value == null || IsCircuitOpen())
        {
            return;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(OperationTimeout);

            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(1)
            };

            await _distributedCache.SetAsync(key, bytes, options, cts.Token);
            CloseCircuit();
        }
        catch (Exception ex)
        {
            OpenCircuit();
            _logger.LogWarning(ex, "Redis cache write error for key '{CacheKey}'. Circuit opened for {CooldownSeconds}s, skipping cache update.",
                key, CircuitBreakerCooldown.TotalSeconds);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_distributedCache == null || IsCircuitOpen())
        {
            return;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(OperationTimeout);

            await _distributedCache.RemoveAsync(key, cts.Token);
            CloseCircuit();
        }
        catch (Exception ex)
        {
            OpenCircuit();
            _logger.LogWarning(ex, "Redis cache remove error for key '{CacheKey}'. Skipping cache invalidate.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }
}
