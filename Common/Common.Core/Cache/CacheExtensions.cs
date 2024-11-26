using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Common.Core.Cache
{
  public static class CacheExtensions
  {
    public static void AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
      var redisConfig = configuration.GetSection("Redis").Get<RedisConfig>();

      services.AddSingleton<IConnectionMultiplexer>(_ =>
          ConnectionMultiplexer.Connect(new ConfigurationOptions
          {
            EndPoints = { redisConfig.ConnectionString },
            AbortOnConnectFail = false,
            AllowAdmin = true,
            ConnectTimeout = 6000,
            SyncTimeout = 6000,
            ConnectRetry = 3
          }));

      services.AddSingleton<ICacheService, RedisCacheService>();
    }
  }
}
