namespace Ride.Api.Shared;

/// <summary>Config bound straight from appsettings — used by CompleteRide.</summary>
public class FareConfig
{
  public double BaseFare { get; set; }
  public double PerKmRate { get; set; }
  public double PerMinuteRate { get; set; }
}
