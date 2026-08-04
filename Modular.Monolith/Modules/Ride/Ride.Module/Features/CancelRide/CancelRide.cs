using System;
using Hikyaku;

namespace Ride.Module.Features.CancelRide;

internal class CancelRide : IRequest
{
  public Guid Id { get; set; }
  public string CancellationReason { get; set; }
  public Guid CallerId { get; set; }
}
