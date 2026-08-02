using Hub.Application.Ports;
using Hub.Infrastructure.Configuration;
using Hub.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hub.Infrastructure.DependencyInjection;

public static class HubInfrastructureExtensions
{
  public static IServiceCollection AddHubInfrastructure(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddScoped<IDriverLocationUpdater, ArbitrerDriverLocationUpdater>();
    services.AddScoped<IRideLocationTracker, ArbitrerRideLocationTracker>();
    services.AddScoped<IActiveRideQuery, ArbitrerActiveRideQuery>();
    services.AddScoped<IRideEtaQuery, ArbitrerRideEtaQuery>();

    services.AddSingleton<IHubConfig>(_ => new HubConfig
    {
      ArrivalThresholdMeters = configuration.GetValue<double>("Hub:ArrivalThresholdMeters")
    });

    return services;
  }
}
