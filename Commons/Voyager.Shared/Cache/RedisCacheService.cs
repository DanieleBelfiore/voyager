using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetTopologySuite.IO.Converters;
using StackExchange.Redis;

namespace Voyager.Shared.Cache;

/// <summary>
/// Distributed caching implementation using Redis.
/// </summary>
public class RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger) : ICacheService
{
  private readonly IDatabase _cache = redis.GetDatabase();

  // GeoJsonConverterFactory is not optional here. The only thing this cache holds is
  // DriverStatusResponse, which carries a NetTopologySuite Point, and plain System.Text.Json
  // cannot write one: Point.Z/M are NaN, so Serialize throws "positive and negative infinity
  // cannot be written as valid JSON" before it ever reaches Redis. That threw on every single
  // write, was swallowed by the catch below, and left the cache permanently empty — every
  // GetDriverStatus went to SQL while the logs filled with the same error.
  private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

  private static JsonSerializerOptions CreateSerializerOptions()
  {
    var options = new JsonSerializerOptions();
    options.Converters.Add(new GeoJsonConverterFactory());

    return options;
  }

  public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
  {
    // Single read instead of ExistsAsync+GetAsync: two round trips left a window where the key
    // could expire between them, and either call failing independently could still report a
    // "hit" that then returned nothing. One StringGetAsync also means a failed/missing read
    // reliably falls through to the factory instead of returning a default value as if cached.
    var (found, cached) = await TryGetAsync<T>(key);
    if (found)
      return cached;

    var value = await factory();

    await SetAsync(key, value, expiration);

    return value;
  }

  public async Task<T> GetAsync<T>(string key)
  {
    var (_, value) = await TryGetAsync<T>(key);

    return value;
  }

  private async Task<(bool Found, T Value)> TryGetAsync<T>(string key)
  {
    try
    {
      var cached = await _cache.StringGetAsync(key);

      if (cached.HasValue)
        return (true, JsonSerializer.Deserialize<T>((string)cached!, SerializerOptions));
    }
    catch (Exception ex)
    {
      // Swallowing is right on the read path: an unreachable Redis or an unreadable payload
      // (stale shape from an older deploy) should degrade to a cache miss, not fail the request.
      logger.LogError(ex, "Error getting value from Redis for key {Key}", key);
    }

    return (false, default);
  }

  private async Task SetAsync<T>(string key, T value, TimeSpan expiration)
  {
    // Serialization deliberately sits outside the try. A type this cache cannot serialize is a
    // bug in the caller, not a transient infrastructure fault, and catching it here is exactly
    // what hid the Point failure above: the cache silently never populated and nothing surfaced
    // beyond a log line. Redis being unreachable is still swallowed — that one is transient.
    var serializedValue = JsonSerializer.Serialize(value, SerializerOptions);

    try
    {
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
