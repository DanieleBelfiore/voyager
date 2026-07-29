using System;
using System.Threading.Tasks;
using CoreCache = Driver.Core.Ports.Secondary.ICacheService;
using SharedCache = Voyager.Shared.Cache.ICacheService;

namespace Driver.Adapters.Secondary.Caching;

public class CacheServiceAdapter(SharedCache inner) : CoreCache
{
  public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration) => inner.GetOrCreateAsync(key, factory, expiration);

  public Task<T> GetAsync<T>(string key) => inner.GetAsync<T>(key);

  public Task RemoveAsync(string key) => inner.RemoveAsync(key);
}
