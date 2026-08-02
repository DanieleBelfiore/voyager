using System;
using Ride.Adapters.Secondary.Configuration;
using Ride.Adapters.Secondary.Messaging;
using Ride.Adapters.Secondary.Persistence;
using Ride.Adapters.Secondary.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IDriverAvailabilityNotifier = Ride.Core.Ports.Secondary.IDriverAvailabilityNotifier;
using IDriverLocationQuery = Ride.Core.Ports.Secondary.IDriverLocationQuery;
using IEtaConfig = Ride.Core.Ports.Secondary.IEtaConfig;
using IFareConfig = Ride.Core.Ports.Secondary.IFareConfig;
using IRatingUpdateService = Ride.Core.Ports.Secondary.IRatingUpdateService;
using IRideEventPublisher = Ride.Core.Ports.Secondary.IRideEventPublisher;
using IRideRepository = Ride.Core.Ports.Secondary.IRideRepository;

namespace Ride.Adapters.Secondary.DependencyInjection;

public static class RideAdaptersExtensions
{
  public static IServiceCollection AddRideSecondaryAdapters(this IServiceCollection services, IConfiguration configuration)
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
    services.AddScoped<IDriverAvailabilityNotifier, ArbitrerDriverAvailabilityNotifier>();

    services.AddSingleton<IEtaConfig>(_ => new EtaConfig
    {
      AverageSpeedKmh = configuration.GetValue<double>("AverageSpeedKmh"),
      MorningPeakMultiplier = configuration.GetValue<double>("MorningPeakMultiplier"),
      EveningPeakMultiplier = configuration.GetValue<double>("EveningPeakMultiplier"),
      NightMultiplier = configuration.GetValue<double>("NightMultiplier"),
      LunchMultiplier = configuration.GetValue<double>("LunchMultiplier")
    });

    services.AddSingleton<IFareConfig>(_ => new FareConfig
    {
      BaseFare = configuration.GetValue<double>("BaseFare"),
      PerKmRate = configuration.GetValue<double>("PerKmRate"),
      PerMinuteRate = configuration.GetValue<double>("PerMinuteRate")
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
