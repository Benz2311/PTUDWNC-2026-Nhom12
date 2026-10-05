using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Caching;

/// <summary>
/// Triển khai ICacheService bao bọc IDistributedCache (Redis) với cơ chế Failover an toàn theo SRS v1.2.0:
/// Khi Redis bị sập hoặc mất kết nối, tự động fallback truy vấn trực tiếp vào DB mà không quăng Exception.
/// Hỗ trợ cả 2 interface ICacheService của Application và Application.Common.
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

    public ResilientCacheService(ILogger<ResilientCacheService> logger, IDistributedCache? distributedCache = null)
    {
        _logger = logger;
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_distributedCache == null)
        {
            return default;
        }

        try
        {
            var cachedBytes = await _distributedCache.GetAsync(key, cancellationToken);
            if (cachedBytes == null || cachedBytes.Length == 0)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedBytes, JsonOptions);
        }
        catch (Exception ex)
        {
            // SRS Mục 4.5 & 6.4: Redis failover - nếu Redis down -> fallback database, không throw exception
            _logger.LogWarning(ex, "Redis cache read error for key '{CacheKey}'. Falling back to database.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (_distributedCache == null || value == null)
        {
            return;
        }

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(1)
            };

            await _distributedCache.SetAsync(key, bytes, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache write error for key '{CacheKey}'. Skipping cache update.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_distributedCache == null)
        {
            return;
        }

        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache remove error for key '{CacheKey}'. Skipping cache invalidate.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // IDistributedCache không hỗ trợ scan theo prefix mặc định;
        // Bắt lỗi an toàn nếu có lỗi
        await Task.CompletedTask;
    }
}
