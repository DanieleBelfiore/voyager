using System;
using MediatR;

namespace Ride.Module.Features.RateRide;

internal class RateRide : IRequest
{
  public Guid RideId { get; set; }
  public int Rating { get; set; }
  public Guid CallerId { get; set; }
}
