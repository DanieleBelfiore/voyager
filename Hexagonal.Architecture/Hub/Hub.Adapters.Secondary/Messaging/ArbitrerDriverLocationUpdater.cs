using System;
using System.Threading;
using System.Threading.Tasks;
using Hub.Core.Ports.Secondary;
using MediatR;
using NetTopologySuite.Geometries;
using Voyager.Contracts.Driver;

namespace Hub.Adapters.Secondary.Messaging;

public class ArbitrerDriverLocationUpdater(IMediator mediator) : IDriverLocationUpdater
{
  public async Task UpdateLocationAsync(Guid driverId, Point location, CancellationToken cancellationToken)
  {
    await mediator.Send(new UpdateLocation { Id = driverId, Location = location }, cancellationToken);
  }
}
