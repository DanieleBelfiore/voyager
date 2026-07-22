using System;
using System.Threading.Tasks;
using ApplicationCache = Driver.Application.Ports.ICacheService;
using SharedCache = Voyager.Shared.Cache.ICacheService;

namespace Driver.Infrastructure.Caching;

/// <summary>
/// Adapts Voyager.Shared's cache abstraction to Application's own port. Application never
/// references Voyager.Shared directly — only Infrastructure is allowed to know it exists.
/// </summary>
public class CacheServiceAdapter(SharedCache inner) : ApplicationCache
{
  public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration) => inner.GetOrCreateAsync(key, factory, expiration);

  public Task<T> GetAsync<T>(string key) => inner.GetAsync<T>(key);

  public Task RemoveAsync(string key) => inner.RemoveAsync(key);
}
