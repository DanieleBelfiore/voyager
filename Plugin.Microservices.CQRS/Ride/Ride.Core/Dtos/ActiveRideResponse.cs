using System;
using NetTopologySuite.Geometries;
using Ride.Core.Enums;

namespace Ride.Core.Dtos
{
  public class ActiveRideResponse
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
  }
}
