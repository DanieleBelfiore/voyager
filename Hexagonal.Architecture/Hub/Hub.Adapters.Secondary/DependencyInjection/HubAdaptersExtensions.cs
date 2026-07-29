using Hub.Adapters.Secondary.Messaging;
using Hub.Core.Ports.Secondary;
using Microsoft.Extensions.DependencyInjection;

namespace Hub.Adapters.Secondary.DependencyInjection;

public static class HubAdaptersExtensions
{
  public static IServiceCollection AddHubSecondaryAdapters(this IServiceCollection services)
  {
    services.AddScoped<IDriverLocationUpdater, ArbitrerDriverLocationUpdater>();
    services.AddScoped<IActiveRideQuery, ArbitrerActiveRideQuery>();
    services.AddScoped<IRideEtaQuery, ArbitrerRideEtaQuery>();

    return services;
  }
}
