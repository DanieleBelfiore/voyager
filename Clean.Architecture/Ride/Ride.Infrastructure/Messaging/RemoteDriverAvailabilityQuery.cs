using System;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Ride.Application.Ports;
using Voyager.Contracts.Driver;

namespace Ride.Infrastructure.Messaging;

public class RemoteDriverAvailabilityQuery(IHikyaku mediator) : IDriverAvailabilityQuery
{
  public async Task<DriverAvailability> GetAvailabilityAsync(Guid driverId, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetDriverAvailability { DriverId = driverId }, cancellationToken);

    return new DriverAvailability(result?.Exists ?? false, result?.IsAvailable ?? false);
  }
}
