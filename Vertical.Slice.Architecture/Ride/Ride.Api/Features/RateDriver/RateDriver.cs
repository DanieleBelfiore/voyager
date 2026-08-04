using System;
using Hikyaku;

namespace Ride.Api.Features.RateDriver;

public class RateDriver : IRequest
{
  public Guid RideId { get; set; }
  public int Rating { get; set; }
  public Guid CallerId { get; set; }
}
