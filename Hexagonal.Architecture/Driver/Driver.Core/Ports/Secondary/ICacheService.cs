using System;
using System.Threading.Tasks;

namespace Driver.Core.Ports.Secondary;

public interface ICacheService
{
  Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
  Task<T> GetAsync<T>(string key);
  Task RemoveAsync(string key);
}
