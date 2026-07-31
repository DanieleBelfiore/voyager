namespace Ride.Core.Ports.Secondary;

public interface IEtaConfig
{
  double AverageSpeedKmh { get; }
  double MorningPeakMultiplier { get; }
  double EveningPeakMultiplier { get; }
  double NightMultiplier { get; }
  double LunchMultiplier { get; }
}
