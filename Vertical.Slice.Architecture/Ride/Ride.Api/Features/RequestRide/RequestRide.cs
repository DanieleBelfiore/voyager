using System;
using Hikyaku;
using NetTopologySuite.Geometries;
using Ride.Api.Shared;

namespace Ride.Api.Features.RequestRide;

public class RequestRide : IRequest<RideDetailsResponse>
{
  public Guid UserId { get; set; }
  public Guid DriverId { get; set; }
  public Point PickupLocation { get; set; }
  public Point DropoffLocation { get; set; }
}
