using System;
using MediatR;

namespace Ride.Core.CQRS.Commands
{
  public class RateDriver : IRequest
  {
    public Guid RideId { get; set; }
    public int Rating { get; set; }
  }
}
