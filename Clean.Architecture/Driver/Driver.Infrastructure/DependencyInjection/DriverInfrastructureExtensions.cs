using System;
using Driver.Infrastructure.Caching;
using Driver.Infrastructure.Configuration;
using Driver.Infrastructure.Messaging;
using Driver.Infrastructure.Persistence;
using Driver.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voyager.Shared.Cache;
using ApplicationCache = Driver.Application.Ports.ICacheService;
using IDriverRepository = Driver.Application.Ports.IDriverRepository;
using IMatchingWeights = Driver.Application.Ports.IMatchingWeights;
using IRatingsQueryService = Driver.Application.Ports.IRatingsQueryService;

namespace Driver.Infrastructure.DependencyInjection;

public static class DriverInfrastructureExtensions
{
  public static IServiceCollection AddDriverInfrastructure(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddDbContext<DriverDbContext>((provider, options) =>
    {
      options.UseSqlServer(configuration.GetConnectionString("DriverContext"), a => a.UseNetTopologySuite());
      options.AddInterceptors(provider.GetRequiredService<SlowQueryInterceptor>());
    });

    services.AddScoped<SlowQueryInterceptor>();

    services.AddScoped<IDriverRepository, DriverRepository>();

    services.AddRedisCache(configuration);
    services.AddScoped<ApplicationCache, CacheServiceAdapter>();

    services.AddScoped<IRatingsQueryService, RemoteRatingsQueryService>();

    services.AddSingleton<IMatchingWeights>(_ => new MatchingWeights
    {
      DistanceWeight = configuration.GetValue<double>("DistanceWeight"),
      RatingWeight = configuration.GetValue<double>("RatingWeight"),
      UserMinRating = configuration.GetValue<double>("UserMinRating"),
      UserMaxRating = configuration.GetValue<double>("UserMaxRating"),
      MaxCandidates = configuration.GetValue<int?>("MaxCandidates") ?? 200
    });

    // Mapperly generates a stateless mapper, but nothing was registering it: every handler that
    // takes one failed to activate at request time with "Unable to resolve service for type
    // DriverMapper" and answered 500. Invisible to the unit tests, which construct the handler
    // themselves and pass a `new DriverMapper()`.
    services.AddSingleton<Driver.Application.Mapping.DriverMapper>();

    return services;
  }

  public static void MigrateDriverDatabase(this IServiceProvider services)
  {
    using var scope = services.CreateScope();
    using var context = scope.ServiceProvider.GetRequiredService<DriverDbContext>();
    context.Database.Migrate();
  }
}
