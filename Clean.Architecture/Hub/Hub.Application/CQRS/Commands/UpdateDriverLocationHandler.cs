using System;
using System.Threading;
using System.Threading.Tasks;
using Hub.Application.Ports;
using Hikyaku;
using NetTopologySuite.Geometries;

namespace Hub.Application.CQRS.Commands;

/// <summary>
/// Same orchestration as VoyagerHub.UpdateDriverLocation in the Plugin.Microservices.CQRS
/// variant, moved out of the SignalR Hub class itself: update the driver's location, find
/// their active ride, and push a location + ETA update to the rider — plus an arrival
/// notification once within the configured threshold of pickup.
/// </summary>
public class UpdateDriverLocationHandler(
  IDriverLocationUpdater locationUpdater,
  IRideLocationTracker rideLocationTracker,
  IActiveRideQuery activeRideQuery,
  IRideEtaQuery etaQuery,
  IHubRelay relay,
  IHubConfig config) : IRequestHandler<UpdateDriverLocation>
{
  public async Task Handle(UpdateDriverLocation request, CancellationToken cancellationToken)
  {
    await locationUpdater.UpdateLocationAsync(request.DriverId, request.Location, cancellationToken);

    var ride = await activeRideQuery.GetActiveRideForDriverAsync(request.DriverId, cancellationToken);
    if (ride == null)
      return;

    // Ride owns its own row, so it has to be told: the driver's position was previously applied
    // to the Driver aggregate only, leaving Ride.LastLocation — the field GET /rides/{id}/location
    // reads — frozen at the pickup point for the whole trip.
    await rideLocationTracker.TrackAsync(ride.Id, request.Location, cancellationToken);

    await relay.SendToRiderNewDriverLocation(ride.Id, request.Location, cancellationToken);

    var eta = await etaQuery.GetEtaAsync(ride.Id, cancellationToken);

    await relay.SendToRiderNewETA(ride.Id, eta.EstimatedArrivalMinutes, eta.DistanceKm, cancellationToken);

    // Only while the driver is still on their way. Once the trip starts, PickupLocation holds
    // wherever the driver was at Start, so this distance measures how far they have driven —
    // under the threshold for the first several hundred metres, which re-fired "driver has
    // arrived" over and over mid-trip.
    if (ride.HasStarted)
      return;

    var distance = DistanceInMeters(ride.PickupLocation, request.Location);
    if (distance < config.ArrivalThresholdMeters)
      await relay.SendToRiderDriverArrival(ride.Id, cancellationToken);
  }

  // NetTopologySuite's Point.Distance() is planar/Cartesian on the raw coordinate values (SRID
  // is metadata only) — on lat/lon points that returns degrees, not meters. Haversine gives the
  // real great-circle distance so it's comparable against config.ArrivalThresholdMeters.
  private static double DistanceInMeters(Point a, Point b)
  {
    const double earthRadiusMeters = 6371000;

    var lat1 = a.Y * Math.PI / 180;
    var lat2 = b.Y * Math.PI / 180;
    var deltaLat = (b.Y - a.Y) * Math.PI / 180;
    var deltaLon = (b.X - a.X) * Math.PI / 180;

    var h = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
            Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

    return 2 * earthRadiusMeters * Math.Asin(Math.Sqrt(h));
  }
}
