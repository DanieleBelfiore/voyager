using System;
using MediatR;

namespace Ride.Api.Features.CancelRide;

public class CancelRide : IRequest
{
  public Guid Id { get; set; }
  public string CancellationReason { get; set; }
  public Guid CallerId { get; set; }
}
