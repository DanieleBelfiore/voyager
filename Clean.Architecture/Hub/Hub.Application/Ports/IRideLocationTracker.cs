using System;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Hub.Application.Ports;

/// <summary>
/// Port for recording the driver's position on the ride itself, owned by the Ride bounded
/// context. Separate from IDriverLocationUpdater on purpose: that one writes the Driver
/// aggregate, and writing only that left Ride.LastLocation — what GET /rides/{id}/location
/// returns — stuck at the pickup point for the whole trip.
/// </summary>
public interface IRideLocationTracker
{
  Task TrackAsync(Guid rideId, Point location, CancellationToken cancellationToken);
}
