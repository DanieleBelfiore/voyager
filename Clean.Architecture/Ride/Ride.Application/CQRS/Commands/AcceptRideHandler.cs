using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using MediatR;

namespace Ride.Application.CQRS.Commands;

public class AcceptRideHandler(IRideRepository repository, IRideEventPublisher events) : IRequestHandler<AcceptRide>
{
  public async Task Handle(AcceptRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdAsync(request.RideId, cancellationToken) ?? throw new Exception("no_ride_found");

    ride.Accept(request.DriverId);

    await repository.SaveChangesAsync(cancellationToken);

    await events.RideAcceptedAsync(ride.Id, cancellationToken);
  }
}
