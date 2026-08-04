using System;
using System.Threading;
using System.Threading.Tasks;
using Hub.Application.Ports;
using Hikyaku;
using NetTopologySuite.Geometries;
using Voyager.Contracts.Driver;

namespace Hub.Infrastructure.Messaging;

public class RemoteDriverLocationUpdater(IHikyaku mediator) : IDriverLocationUpdater
{
  public async Task UpdateLocationAsync(Guid driverId, Point location, CancellationToken cancellationToken)
  {
    await mediator.Send(new UpdateLocation { Id = driverId, Location = location }, cancellationToken);
  }
}
