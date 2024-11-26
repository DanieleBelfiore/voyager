using System;
using MediatR;

namespace Ride.Core.CQRS.Commands
{
  public class AcceptRide : IRequest
  {
    public Guid DriverId { get; set; }
    public Guid RideId { get; set; }
  }
}
