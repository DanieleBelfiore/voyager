using System;
using System.Threading.Tasks;

namespace Driver.Application.Ports;

/// <summary>
/// Application-owned cache port. Deliberately not a reference to Voyager.Shared's cache
/// abstraction: Application must not depend on anything outside Domain + itself, even an
/// infra-agnostic one — Infrastructure is the layer that's allowed to adapt to Voyager.Shared.
/// </summary>
public interface ICacheService
{
  Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
  Task<T> GetAsync<T>(string key);
  Task RemoveAsync(string key);
}
