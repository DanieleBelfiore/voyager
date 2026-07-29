using System;
using MediatR;

namespace Ride.Module.Features.CancelRide;

internal class CancelRide : IRequest
{
  public Guid Id { get; set; }
  public string CancellationReason { get; set; }
}
