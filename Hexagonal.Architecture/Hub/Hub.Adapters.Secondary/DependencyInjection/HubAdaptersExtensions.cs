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
    services.AddScoped<IDriverLocationUpdater, ArbitrerDriverLocationUpdater>();
    services.AddScoped<IActiveRideQuery, ArbitrerActiveRideQuery>();
    services.AddScoped<IRideEtaQuery, ArbitrerRideEtaQuery>();

    services.AddSingleton<IHubConfig>(_ => new HubConfig
    {
      ArrivalThresholdMeters = configuration.GetValue<double>("Hub:ArrivalThresholdMeters")
    });

    return services;
  }
}
