using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Common.Core.Cache
{
  /// <summary>
  /// Distributed caching implementation using Redis.
  /// Provides:
  /// - High-performance data access for frequently used data
  /// - Data consistency across multiple service instances
  /// - Automatic cache invalidation for stale data
  /// - Configurable expiration policies
  /// </summary>
  public class RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger) : ICacheService
  {
    private readonly IDatabase _cache = redis.GetDatabase();

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
      var value = await GetAsync<T>(key);
      if (value != null)
        return value;

      value = await factory();

      await SetAsync(key, value, expiration);

      return value;
    }

    public async Task<T> GetAsync<T>(string key)
    {
      try
      {
        var value = await _cache.StringGetAsync(key);

        return !value.HasValue ? default : JsonSerializer.Deserialize<T>(value);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error getting value from Redis for key {Key}", key);

        return default;
      }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
      try
      {
        var serializedValue = JsonSerializer.Serialize(value);
        await _cache.StringSetAsync(key, serializedValue, expiration);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error setting value in Redis for key {Key}", key);
      }
    }

    public async Task RemoveAsync(string key)
    {
      try
      {
        await _cache.KeyDeleteAsync(key);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error removing key {Key} from Redis", key);
      }
    }

    public async Task<bool> ExistsAsync(string key)
    {
      try
      {
        return await _cache.KeyExistsAsync(key);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error checking existence of key {Key} in Redis", key);

        return false;
      }
    }
  }
}
