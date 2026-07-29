using System;
using MediatR;
using Ride.Core.Dtos;

namespace Ride.Core.CQRS.Queries
{
  public class GetActiveRide : IRequest<ActiveRideResponse>
  {
    public Guid? DriverId { get; set; }
    public Guid? UserId { get; set; }
  }
}
