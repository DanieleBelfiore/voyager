using System;
using Hikyaku;

namespace Ride.Core.CQRS.Commands
{
  public class RateRide : IRequest
  {
    public Guid RideId { get; set; }
    public Guid CallerId { get; set; }
    public int Rating { get; set; }
  }
}
