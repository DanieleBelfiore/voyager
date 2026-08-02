using Ride.Core.Ports.Secondary;

namespace Ride.Adapters.Secondary.Configuration;

public class FareConfig : IFareConfig
{
  public double BaseFare { get; set; }
  public double PerKmRate { get; set; }
  public double PerMinuteRate { get; set; }
}
