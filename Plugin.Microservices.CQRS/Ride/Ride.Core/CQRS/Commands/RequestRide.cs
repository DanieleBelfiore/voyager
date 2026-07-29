using System;
using MediatR;
using NetTopologySuite.Geometries;
using Ride.Core.Dtos;

namespace Ride.Core.CQRS.Commands
{
  public class RequestRide : IRequest<RideDetailsResponse>
  {
    public Guid UserId { get; set; }
    public Guid DriverId { get; set; }
    public Point PickupLocation { get; set; }
    public Point DropoffLocation { get; set; }
  }
}
