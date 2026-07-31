using Ride.Application.Ports;

namespace Ride.Infrastructure.Configuration;

public class EtaConfig : IEtaConfig
{
  public double AverageSpeedKmh { get; set; }
  public double MorningPeakMultiplier { get; set; }
  public double EveningPeakMultiplier { get; set; }
  public double NightMultiplier { get; set; }
  public double LunchMultiplier { get; set; }
}
