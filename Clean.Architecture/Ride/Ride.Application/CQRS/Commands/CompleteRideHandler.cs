using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using MediatR;

namespace Ride.Application.CQRS.Commands;

public class CompleteRideHandler(IRideRepository repository, IRideEventPublisher events) : IRequestHandler<CompleteRide>
{
  public async Task Handle(CompleteRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new Exception("no_ride_found");

    ride.Complete(request.Location, request.Price);

    await repository.SaveChangesAsync(cancellationToken);

    await events.RideCompletedAsync(ride.Id, cancellationToken);
  }
}
