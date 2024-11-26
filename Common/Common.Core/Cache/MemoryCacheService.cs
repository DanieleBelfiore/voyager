using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Common.Core.Cache
{
  public class MemoryCacheService : ICacheService
  {
    private readonly ConcurrentDictionary<string, (object Value, DateTime? Expiration)> _cache = new();

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
      if (await ExistsAsync(key))
      {
        var value = await GetAsync<T>(key);
        if (value != null)
          return value;
      }

      var newValue = await factory();
      await SetAsync(key, newValue, expiration);
      return newValue;
    }

    public Task<T> GetAsync<T>(string key)
    {
      if (!_cache.TryGetValue(key, out var entry))
        return Task.FromResult<T>(default);

      if (!entry.Expiration.HasValue || entry.Expiration.Value >= DateTime.UtcNow)
        return Task.FromResult((T)entry.Value);

      _cache.TryRemove(key, out _);

      return Task.FromResult<T>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
      DateTime? expirationTime = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null;
      _cache.AddOrUpdate(key, (value, expirationTime), (_, _) => (value, expirationTime));

      return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
      _cache.TryRemove(key, out _);

      return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
      if (!_cache.TryGetValue(key, out var entry))
        return Task.FromResult(false);

      if (!entry.Expiration.HasValue || entry.Expiration.Value >= DateTime.UtcNow)
        return Task.FromResult(true);

      _cache.TryRemove(key, out _);

      return Task.FromResult(false);
    }

    public void Clear()
    {
      _cache.Clear();
    }
  }
}
