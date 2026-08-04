using System;
using Hikyaku;

namespace Ride.Core.CQRS.Commands
{
  public class RateDriver : IRequest
  {
    public Guid RideId { get; set; }
    public Guid CallerId { get; set; }
    public int Rating { get; set; }
  }
}
