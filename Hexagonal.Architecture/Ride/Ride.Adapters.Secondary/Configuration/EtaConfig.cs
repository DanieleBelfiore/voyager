using Ride.Core.Ports.Secondary;

namespace Ride.Adapters.Secondary.Configuration;

public class EtaConfig : IEtaConfig
{
  public double AverageSpeedKmh { get; set; }
  public double MorningPeakMultiplier { get; set; }
  public double EveningPeakMultiplier { get; set; }
  public double NightMultiplier { get; set; }
  public double LunchMultiplier { get; set; }
}
