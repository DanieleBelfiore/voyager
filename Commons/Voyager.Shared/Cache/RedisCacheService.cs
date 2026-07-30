using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Voyager.Shared.Cache;

/// <summary>
/// Distributed caching implementation using Redis.
/// </summary>
public class RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger) : ICacheService
{
  private readonly IDatabase _cache = redis.GetDatabase();

  public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
  {
    if (await ExistsAsync(key))
      return await GetAsync<T>(key);

    var value = await factory();

    await SetAsync(key, value, expiration);

    return value;
  }

  public async Task<T> GetAsync<T>(string key)
  {
    try
    {
      var value = await _cache.StringGetAsync(key);

      return !value.HasValue ? default : JsonSerializer.Deserialize<T>(((string)value)!);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting value from Redis for key {Key}", key);

      return default;
    }
  }

  private async Task SetAsync<T>(string key, T value, TimeSpan expiration)
  {
    try
    {
      var serializedValue = JsonSerializer.Serialize(value);
      await _cache.StringSetAsync(key, serializedValue, new Expiration(expiration));
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
