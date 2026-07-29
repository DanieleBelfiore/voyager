namespace Ride.Module.Entities;

/// <summary>Public, unlike Ride — it's part of RideDetailsResponse/ActiveRideResponse, both public DTOs (CS0050).</summary>
public enum RideStatus
{
  Requested,
  DriverAssigned,
  InProgress,
  Completed,
  Cancelled
}
