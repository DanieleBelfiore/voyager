using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Ports.Secondary;
using MediatR;
using NetTopologySuite.Geometries;
using Voyager.Contracts.Driver;

namespace Ride.Adapters.Secondary.Messaging;

public class ArbitrerDriverLocationQuery(IMediator mediator) : IDriverLocationQuery
{
  public async Task<Point> GetLocationAsync(Guid driverId, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetDriverLocation { DriverId = driverId }, cancellationToken);

    return result?.LastLocation;
  }
}
