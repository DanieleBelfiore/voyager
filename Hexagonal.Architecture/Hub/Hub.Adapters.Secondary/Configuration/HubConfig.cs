using Hub.Core.Ports.Secondary;

namespace Hub.Adapters.Secondary.Configuration;

public class HubConfig : IHubConfig
{
  public double ArrivalThresholdMeters { get; set; }
}
