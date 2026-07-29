using System;
using Driver.Adapters.Secondary.Caching;
using Driver.Adapters.Secondary.Configuration;
using Driver.Adapters.Secondary.Messaging;
using Driver.Adapters.Secondary.Persistence;
using Driver.Adapters.Secondary.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voyager.Shared.Cache;
using CoreCache = Driver.Core.Ports.Secondary.ICacheService;
using IDriverRepository = Driver.Core.Ports.Secondary.IDriverRepository;
using IMatchingWeights = Driver.Core.Ports.Secondary.IMatchingWeights;
using IRatingsQueryService = Driver.Core.Ports.Secondary.IRatingsQueryService;

namespace Driver.Adapters.Secondary.DependencyInjection;

/// <summary>Registers every secondary adapter behind Driver.Core's secondary ports.</summary>
public static class DriverAdaptersExtensions
{
  public static IServiceCollection AddDriverSecondaryAdapters(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddDbContext<DriverDbContext>((provider, options) =>
    {
      options.UseSqlServer(configuration.GetConnectionString("DriverContext"), a => a.UseNetTopologySuite());
      options.AddInterceptors(provider.GetRequiredService<SlowQueryInterceptor>());
    });

    services.AddScoped<SlowQueryInterceptor>();

    services.AddScoped<IDriverRepository, DriverRepository>();

    services.AddRedisCache(configuration);
    services.AddScoped<CoreCache, CacheServiceAdapter>();

    services.AddScoped<IRatingsQueryService, ArbitrerRatingsQueryService>();

    services.AddSingleton<IMatchingWeights>(_ => new MatchingWeights
    {
      DistanceWeight = configuration.GetValue<double>("DistanceWeight"),
      RatingWeight = configuration.GetValue<double>("RatingWeight"),
      UserMinRating = configuration.GetValue<double>("UserMinRating"),
      UserMaxRating = configuration.GetValue<double>("UserMaxRating")
    });

    return services;
  }

  public static void MigrateDriverDatabase(this IServiceProvider services)
  {
    using var scope = services.CreateScope();
    using var context = scope.ServiceProvider.GetRequiredService<DriverDbContext>();
    context.Database.Migrate();
  }
}
