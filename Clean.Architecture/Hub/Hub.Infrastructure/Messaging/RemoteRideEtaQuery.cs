using System;
using System.Threading;
using System.Threading.Tasks;
using Hub.Application.Ports;
using Hikyaku;
using Voyager.Contracts.Ride;

namespace Hub.Infrastructure.Messaging;

public class RemoteRideEtaQuery(IHikyaku mediator) : IRideEtaQuery
{
  public async Task<RideEta> GetEtaAsync(Guid rideId, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetRideETA { Id = rideId }, cancellationToken);

    return new RideEta { EstimatedArrivalMinutes = result.EstimatedArrivalMinutes, DistanceKm = result.DistanceKm };
  }
}
