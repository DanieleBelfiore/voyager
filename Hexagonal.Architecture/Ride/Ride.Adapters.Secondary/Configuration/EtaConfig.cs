using Ride.Core.Ports.Secondary;

namespace Ride.Adapters.Secondary.Configuration;

public class EtaConfig : IEtaConfig
{
  public double AverageSpeedKmh { get; set; }
}
