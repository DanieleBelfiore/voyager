namespace Ride.Module.Shared;

internal class EtaConfig
{
  public double AverageSpeedKmh { get; set; }
  public double MorningPeakMultiplier { get; set; }
  public double EveningPeakMultiplier { get; set; }
  public double NightMultiplier { get; set; }
  public double LunchMultiplier { get; set; }
}
