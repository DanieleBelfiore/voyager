using System;
using MediatR;

namespace Ride.Core.CQRS.Commands
{
  public class CancelRide : IRequest
  {
    public Guid Id { get; set; }
    public string CancellationReason { get; set; }
  }
}
