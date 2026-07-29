using System;
using System.Threading;
using System.Threading.Tasks;
using Hub.Application.Ports;
using MediatR;
using NetTopologySuite.Geometries;
using Voyager.Contracts.Driver;

namespace Hub.Infrastructure.Messaging;

public class ArbitrerDriverLocationUpdater(IMediator mediator) : IDriverLocationUpdater
{
  public async Task UpdateLocationAsync(Guid driverId, Point location, CancellationToken cancellationToken)
  {
    await mediator.Send(new UpdateLocation { Id = driverId, Location = location }, cancellationToken);
  }
}
