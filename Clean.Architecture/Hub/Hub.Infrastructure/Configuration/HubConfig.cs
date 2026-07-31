using Hub.Application.Ports;

namespace Hub.Infrastructure.Configuration;

public class HubConfig : IHubConfig
{
  public double ArrivalThresholdMeters { get; set; }
}
