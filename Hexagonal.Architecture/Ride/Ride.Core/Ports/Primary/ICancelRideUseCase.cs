using System;
using MediatR;

namespace Ride.Core.Ports.Primary;

public class CancelRide : IRequest
{
  public Guid Id { get; set; }
  public string CancellationReason { get; set; }
}

public interface ICancelRideUseCase : IRequestHandler<CancelRide>;
