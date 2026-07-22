using Hub.Application.Ports;
using Hub.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Hub.Infrastructure.DependencyInjection;

public static class HubInfrastructureExtensions
{
  public static IServiceCollection AddHubInfrastructure(this IServiceCollection services)
  {
    services.AddScoped<IDriverLocationUpdater, ArbitrerDriverLocationUpdater>();
    services.AddScoped<IActiveRideQuery, ArbitrerActiveRideQuery>();
    services.AddScoped<IRideEtaQuery, ArbitrerRideEtaQuery>();

    return services;
  }
}
