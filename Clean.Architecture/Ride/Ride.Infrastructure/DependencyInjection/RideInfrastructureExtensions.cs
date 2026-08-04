using System;
using Ride.Infrastructure.Configuration;
using Ride.Infrastructure.Messaging;
using Ride.Infrastructure.Persistence;
using Ride.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IDriverAvailabilityNotifier = Ride.Application.Ports.IDriverAvailabilityNotifier;
using IDriverAvailabilityQuery = Ride.Application.Ports.IDriverAvailabilityQuery;
using IDriverLocationQuery = Ride.Application.Ports.IDriverLocationQuery;
using IEtaConfig = Ride.Application.Ports.IEtaConfig;
using IFareConfig = Ride.Application.Ports.IFareConfig;
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

    services.AddScoped<IRatingUpdateService, RemoteRatingUpdateService>();
    services.AddScoped<IDriverLocationQuery, RemoteDriverLocationQuery>();
    services.AddScoped<IDriverAvailabilityQuery, RemoteDriverAvailabilityQuery>();
    services.AddScoped<IRideEventPublisher, RemoteRideEventPublisher>();
    services.AddScoped<IDriverAvailabilityNotifier, RemoteDriverAvailabilityNotifier>();

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

    // Mapperly generates a stateless mapper, but nothing was registering it: every handler that
    // takes one failed to activate at request time with "Unable to resolve service for type
    // RideMapper" and answered 500. Invisible to the unit tests, which construct the handler
    // themselves and pass a `new RideMapper()`.
    services.AddSingleton<Ride.Application.Mapping.RideMapper>();

    return services;
  }

  public static void MigrateRideDatabase(this IServiceProvider services)
  {
    using var scope = services.CreateScope();
    using var context = scope.ServiceProvider.GetRequiredService<RideDbContext>();
    context.Database.Migrate();
  }
}
