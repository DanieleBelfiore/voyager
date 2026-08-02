using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Ride.Core.Ports.Secondary;
using Voyager.Contracts.Driver;

namespace Ride.Adapters.Secondary.Messaging;

public class ArbitrerDriverAvailabilityQuery(IMediator mediator) : IDriverAvailabilityQuery
{
  public async Task<DriverAvailability> GetAvailabilityAsync(Guid driverId, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetDriverAvailability { DriverId = driverId }, cancellationToken);

    return new DriverAvailability(result?.Exists ?? false, result?.IsAvailable ?? false);
  }
}
