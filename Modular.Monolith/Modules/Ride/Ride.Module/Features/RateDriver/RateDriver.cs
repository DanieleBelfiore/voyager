using System;
using Hikyaku;

namespace Ride.Module.Features.RateDriver;

internal class RateDriver : IRequest
{
  public Guid RideId { get; set; }
  public int Rating { get; set; }
  public Guid CallerId { get; set; }
}
