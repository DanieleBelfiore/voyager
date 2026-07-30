using System;
using MediatR;

namespace Ride.Application.CQRS.Commands;

public class RateDriver : IRequest
{
  public Guid RideId { get; set; }
  public int Rating { get; set; }
  public Guid CallerId { get; set; }
}
