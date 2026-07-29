using Voyager.Shared.Cache;

namespace Driver.Tests;

public class FakeCacheService : ICacheService
{
  private readonly Dictionary<string, object> _store = new();

  public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
  {
    if (_store.TryGetValue(key, out var cached))
      return Task.FromResult((T)cached);

    return CreateAndStore(key, factory);
  }

  private async Task<T> CreateAndStore<T>(string key, Func<Task<T>> factory)
  {
    var value = await factory();
    _store[key] = value!;
    return value;
  }

  public Task<T> GetAsync<T>(string key)
  {
    return Task.FromResult(_store.TryGetValue(key, out var value) ? (T)value : default!);
  }

  public Task RemoveAsync(string key)
  {
    _store.Remove(key);
    return Task.CompletedTask;
  }
}
