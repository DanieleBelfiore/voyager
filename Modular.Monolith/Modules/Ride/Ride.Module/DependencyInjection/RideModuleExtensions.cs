using System;
using Ride.Module.Persistence;
using Ride.Module.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ride.Module.DependencyInjection;

public static class RideModuleExtensions
{
  public static IServiceCollection AddRideModule(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddDbContext<RideDbContext>((provider, options) =>
    {
      options.UseSqlServer(configuration.GetConnectionString("RideContext"), a => a.UseNetTopologySuite());
      options.AddInterceptors(provider.GetRequiredService<SlowQueryInterceptor>());
    });

    services.AddScoped<SlowQueryInterceptor>();

    services.AddHealthChecks().AddDbContextCheck<RideDbContext>("ride-database", tags: ["ready"]);

    services.Configure<EtaConfig>(configuration);
    services.Configure<FareConfig>(configuration);

    return services;
  }

  public static void MigrateRideDatabase(this IServiceProvider services)
  {
    using var scope = services.CreateScope();
    using var context = scope.ServiceProvider.GetRequiredService<RideDbContext>();
    context.Database.Migrate();
  }
}
