using System;
using Hikyaku;

namespace Ride.Api.Features.RateRide;

public class RateRide : IRequest
{
  public Guid RideId { get; set; }
  public int Rating { get; set; }
  public Guid CallerId { get; set; }
}
