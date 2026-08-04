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
    services.AddScoped<IDriverLocationUpdater, RemoteDriverLocationUpdater>();
    services.AddScoped<IRideLocationTracker, RemoteRideLocationTracker>();
    services.AddScoped<IActiveRideQuery, RemoteActiveRideQuery>();
    services.AddScoped<IRideEtaQuery, RemoteRideEtaQuery>();

    services.AddSingleton<IHubConfig>(_ => new HubConfig
    {
      ArrivalThresholdMeters = configuration.GetValue<double>("Hub:ArrivalThresholdMeters")
    });

    return services;
  }
}
