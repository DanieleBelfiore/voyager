using System;
using Hikyaku;
using NetTopologySuite.Geometries;
using Ride.Application.Dtos;

namespace Ride.Application.CQRS.Commands;

public class RequestRide : IRequest<RideDetailsResponse>
{
  public Guid UserId { get; set; }
  public Guid DriverId { get; set; }
  public Point PickupLocation { get; set; }
  public Point DropoffLocation { get; set; }
}
