using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using MediatR;

namespace Ride.Application.CQRS.Commands;

public class StartRideHandler(IRideRepository repository) : IRequestHandler<StartRide>
{
  public async Task Handle(StartRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new Exception("no_ride_found");

    ride.Start(request.Location);

    await repository.SaveChangesAsync(cancellationToken);
  }
}
