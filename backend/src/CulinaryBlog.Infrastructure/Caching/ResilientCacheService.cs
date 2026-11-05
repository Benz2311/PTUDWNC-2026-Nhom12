using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Caching;

public sealed class ResilientCacheService :
    CulinaryBlog.Application.Common.Interfaces.ICacheService,
    CulinaryBlog.Application.Interfaces.ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    private static readonly TimeSpan CircuitBreakerCooldown = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromMilliseconds(500);

    private readonly IDistributedCache? _distributedCache;
    private readonly ILogger<ResilientCacheService> _logger;
    private long _circuitBreakerUntilTicks;

    public ResilientCacheService(
        ILogger<ResilientCacheService> logger,
        IDistributedCache? distributedCache = null)
    {
        _logger = logger;
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_distributedCache is null || IsCircuitOpen())
        {
            return default;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(OperationTimeout);
            var cachedBytes = await _distributedCache.GetAsync(key, timeout.Token);
            CloseCircuit();
            return cachedBytes is { Length: > 0 }
                ? JsonSerializer.Deserialize<T>(cachedBytes, JsonOptions)
                : default;
        }
        catch (Exception exception)
        {
            OpenCircuit();
            _logger.LogWarning(
                exception,
                "Redis cache read error for key '{CacheKey}'. Circuit opened for {CooldownSeconds}s.",
                key,
                CircuitBreakerCooldown.TotalSeconds);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (_distributedCache is null || value is null || IsCircuitOpen())
        {
            return;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(OperationTimeout);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(1),
            };
            await _distributedCache.SetAsync(key, bytes, options, timeout.Token);
            CloseCircuit();
        }
        catch (Exception exception)
        {
            OpenCircuit();
            _logger.LogWarning(
                exception,
                "Redis cache write error for key '{CacheKey}'. Circuit opened for {CooldownSeconds}s.",
                key,
                CircuitBreakerCooldown.TotalSeconds);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_distributedCache is null || IsCircuitOpen())
        {
            return;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(OperationTimeout);
            await _distributedCache.RemoveAsync(key, timeout.Token);
            CloseCircuit();
        }
        catch (Exception exception)
        {
            OpenCircuit();
            _logger.LogWarning(exception, "Redis cache remove error for key '{CacheKey}'.", key);
        }
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    private bool IsCircuitOpen()
    {
        var retryAt = Interlocked.Read(ref _circuitBreakerUntilTicks);
        return retryAt > DateTime.UtcNow.Ticks;
    }

    private void OpenCircuit() =>
        Interlocked.Exchange(ref _circuitBreakerUntilTicks, DateTime.UtcNow.Add(CircuitBreakerCooldown).Ticks);

    private void CloseCircuit() => Interlocked.Exchange(ref _circuitBreakerUntilTicks, 0);
}
