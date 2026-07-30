using System;
using MediatR;

namespace Ride.Application.CQRS.Commands;

public class RateRide : IRequest
{
  public Guid RideId { get; set; }
  public int Rating { get; set; }
  public Guid CallerId { get; set; }
}
