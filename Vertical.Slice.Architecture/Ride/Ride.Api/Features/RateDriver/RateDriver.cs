using System;
using MediatR;

namespace Ride.Api.Features.RateDriver;

public class RateDriver : IRequest
{
  public Guid RideId { get; set; }
  public int Rating { get; set; }
}
