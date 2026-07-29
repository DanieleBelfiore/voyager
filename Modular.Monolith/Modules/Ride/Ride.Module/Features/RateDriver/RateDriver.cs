using System;
using MediatR;

namespace Ride.Module.Features.RateDriver;

internal class RateDriver : IRequest
{
  public Guid RideId { get; set; }
  public int Rating { get; set; }
}
