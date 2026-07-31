using System;
using Ride.Infrastructure.Configuration;
using Ride.Infrastructure.Messaging;
using Ride.Infrastructure.Persistence;
using Ride.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IDriverLocationQuery = Ride.Application.Ports.IDriverLocationQuery;
using IEtaConfig = Ride.Application.Ports.IEtaConfig;
using IRatingUpdateService = Ride.Application.Ports.IRatingUpdateService;
using IRideEventPublisher = Ride.Application.Ports.IRideEventPublisher;
using IRideRepository = Ride.Application.Ports.IRideRepository;

namespace Ride.Infrastructure.DependencyInjection;

public static class RideInfrastructureExtensions
{
  public static IServiceCollection AddRideInfrastructure(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddDbContext<RideDbContext>((provider, options) =>
    {
      options.UseSqlServer(configuration.GetConnectionString("RideContext"), a => a.UseNetTopologySuite());
      options.AddInterceptors(provider.GetRequiredService<SlowQueryInterceptor>());
    });

    services.AddScoped<SlowQueryInterceptor>();

    services.AddScoped<IRideRepository, RideRepository>();

    services.AddScoped<IRatingUpdateService, ArbitrerRatingUpdateService>();
    services.AddScoped<IDriverLocationQuery, ArbitrerDriverLocationQuery>();
    services.AddScoped<IRideEventPublisher, ArbitrerRideEventPublisher>();

    services.AddSingleton<IEtaConfig>(_ => new EtaConfig
    {
      AverageSpeedKmh = configuration.GetValue<double>("AverageSpeedKmh"),
      MorningPeakMultiplier = configuration.GetValue<double>("MorningPeakMultiplier"),
      EveningPeakMultiplier = configuration.GetValue<double>("EveningPeakMultiplier"),
      NightMultiplier = configuration.GetValue<double>("NightMultiplier"),
      LunchMultiplier = configuration.GetValue<double>("LunchMultiplier")
    });

    return services;
  }

  public static void MigrateRideDatabase(this IServiceProvider services)
  {
    using var scope = services.CreateScope();
    using var context = scope.ServiceProvider.GetRequiredService<RideDbContext>();
    context.Database.Migrate();
  }
}
