namespace Ride.Api.Features.GetRideETA;

public class ETAResponse
{
  public int? EstimatedArrivalMinutes { get; set; }
  public double? DistanceKm { get; set; }
}
