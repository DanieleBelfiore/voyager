using System;
using System.Threading;
using System.Threading.Tasks;
using Hub.Core.Ports.Secondary;
using Hub.Core.Ports.Primary;
using NetTopologySuite.Geometries;

namespace Hub.Core.UseCases;

/// <summary>
/// Update the driver's location, find their active ride, and push a location + ETA update to
/// the rider — plus an arrival notification once within the configured threshold of pickup.
/// </summary>
public class UpdateDriverLocationUseCase(
  IDriverLocationUpdater locationUpdater,
  IActiveRideQuery activeRideQuery,
  IRideEtaQuery etaQuery,
  IHubRelay relay,
  IHubConfig config) : IUpdateDriverLocationUseCase
{
  public async Task Handle(UpdateDriverLocation request, CancellationToken cancellationToken)
  {
    await locationUpdater.UpdateLocationAsync(request.DriverId, request.Location, cancellationToken);

    var ride = await activeRideQuery.GetActiveRideForDriverAsync(request.DriverId, cancellationToken);
    if (ride == null)
      return;

    await relay.SendToRiderNewDriverLocation(ride.Id, request.Location, cancellationToken);

    var eta = await etaQuery.GetEtaAsync(ride.Id, cancellationToken);

    await relay.SendToRiderNewETA(ride.Id, eta.EstimatedArrivalMinutes, eta.DistanceKm, cancellationToken);

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
