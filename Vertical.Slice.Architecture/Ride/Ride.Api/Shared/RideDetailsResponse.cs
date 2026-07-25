using System;
using NetTopologySuite.Geometries;
using Ride.Api.Entities;
using RideEntity = Ride.Api.Entities.Ride;

namespace Ride.Api.Shared;

/// <summary>Shared response shape — read by GetRideDetails, RequestRide, GetRideHistory and GetRideDriverHistory.</summary>
public class RideDetailsResponse
{
  public Guid Id { get; set; }
  public Guid UserId { get; set; }
  public Guid DriverId { get; set; }
  public DateTime RequestedAt { get; set; }
  public DateTime? StartAt { get; set; }
  public DateTime? EndAt { get; set; }
  public double? Price { get; set; }
  public RideStatus Status { get; set; }
  public string CancellationReason { get; set; }
  public Point PickupLocation { get; set; }
  public Point DropoffLocation { get; set; }
  public Point LastLocation { get; set; }
  public DateTime LastUpdateDate { get; set; }

  public static RideDetailsResponse From(RideEntity ride) => new()
  {
    Id = ride.Id,
    UserId = ride.UserId,
    DriverId = ride.DriverId,
    RequestedAt = ride.RequestedAt,
    StartAt = ride.StartAt,
    EndAt = ride.EndAt,
    Price = ride.Price,
    Status = ride.Status,
    CancellationReason = ride.CancellationReason,
    PickupLocation = ride.PickupLocation,
    DropoffLocation = ride.DropoffLocation,
    LastLocation = ride.LastLocation,
    LastUpdateDate = ride.LastUpdateDate
  };
}
