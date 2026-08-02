using Ride.Application.Ports;

namespace Ride.Infrastructure.Configuration;

public class FareConfig : IFareConfig
{
  public double BaseFare { get; set; }
  public double PerKmRate { get; set; }
  public double PerMinuteRate { get; set; }
}
