using Hub.Adapters.Secondary.Configuration;
using Hub.Adapters.Secondary.Messaging;
using Hub.Core.Ports.Secondary;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hub.Adapters.Secondary.DependencyInjection;

public static class HubAdaptersExtensions
{
  public static IServiceCollection AddHubSecondaryAdapters(this IServiceCollection services, IConfiguration configuration)
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
