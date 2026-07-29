namespace Ride.Api.Shared;

/// <summary>Config bound straight from appsettings — shared by GetRideETA and GetRideETAForHub.</summary>
public class EtaConfig
{
  public double AverageSpeedKmh { get; set; }
}
