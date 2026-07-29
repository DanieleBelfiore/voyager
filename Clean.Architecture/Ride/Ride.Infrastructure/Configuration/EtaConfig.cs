using Ride.Application.Ports;

namespace Ride.Infrastructure.Configuration;

public class EtaConfig : IEtaConfig
{
  public double AverageSpeedKmh { get; set; }
}
