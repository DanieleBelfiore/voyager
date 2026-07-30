using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using MediatR;

namespace Ride.Application.CQRS.Commands;

public class CancelRideHandler(IRideRepository repository, IRideEventPublisher events) : IRequestHandler<CancelRide>
{
  public async Task Handle(CancelRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new Exception("no_ride_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    ride.Cancel(request.CancellationReason);

    await repository.SaveChangesAsync(cancellationToken);

    await events.RideCancelledAsync(ride.Id, cancellationToken);
  }
}
