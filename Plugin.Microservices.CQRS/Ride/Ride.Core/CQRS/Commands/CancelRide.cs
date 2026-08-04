using System;
using Hikyaku;

namespace Ride.Core.CQRS.Commands
{
  public class CancelRide : IRequest
  {
    public Guid Id { get; set; }
    public Guid CallerId { get; set; }
    public string CancellationReason { get; set; }
  }
}
