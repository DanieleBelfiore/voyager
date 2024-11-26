using System;
using MediatR;

namespace Ride.Core.CQRS.Commands
{
  public class RateRide : IRequest
  {
    public Guid RideId { get; set; }
    public int Rating { get; set; }
  }
}
