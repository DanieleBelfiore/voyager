namespace Ride.Api.Shared;

/// <summary>Config bound straight from appsettings — shared by GetRideETA and GetRideETAForHub.</summary>
public class EtaConfig
{
  public double AverageSpeedKmh { get; set; }
  public double MorningPeakMultiplier { get; set; }
  public double EveningPeakMultiplier { get; set; }
  public double NightMultiplier { get; set; }
  public double LunchMultiplier { get; set; }
}
