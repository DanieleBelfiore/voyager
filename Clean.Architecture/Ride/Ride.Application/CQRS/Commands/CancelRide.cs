using System;
using MediatR;

namespace Ride.Application.CQRS.Commands;

public class CancelRide : IRequest
{
  public Guid Id { get; set; }
  public string CancellationReason { get; set; }
  public Guid CallerId { get; set; }
}
