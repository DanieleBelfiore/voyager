using System;
using System.Threading;
using System.Threading.Tasks;
using Hub.Core.Ports.Secondary;
using Hikyaku;
using NetTopologySuite.Geometries;
using Voyager.Contracts.Ride;

namespace Hub.Adapters.Secondary.Messaging;

public class RemoteRideLocationTracker(IHikyaku mediator) : IRideLocationTracker
{
  public async Task TrackAsync(Guid rideId, Point location, CancellationToken cancellationToken)
  {
    await mediator.Send(new TrackRideLocation { RideId = rideId, Location = location }, cancellationToken);
  }
}
