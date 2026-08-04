using System;
using System.Threading;
using System.Threading.Tasks;
using Hub.Api.Shared;
using Hikyaku;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using Voyager.Contracts.Driver;
using Voyager.Contracts.Ride;

namespace Hub.Api.Features.UpdateDriverLocation;

/// <summary>
/// Update the driver's location, find their active ride, and push a location + ETA update to
/// the rider — plus an arrival notification once within the configured threshold of pickup. No
/// port for any of this — IHikyaku.Send reaches Driver/Ride remotely via Kaido, IHubContext
/// pushes over SignalR directly.
/// </summary>
public class UpdateDriverLocationHandler(IHikyaku mediator, IHubContext<VoyagerHub, IVoyagerShareClient> hub, IConfiguration configuration) : IRequestHandler<UpdateDriverLocation>
{
  public async Task Handle(UpdateDriverLocation request, CancellationToken cancellationToken)
  {
    await mediator.Send(new Voyager.Contracts.Driver.UpdateLocation { Id = request.DriverId, Location = request.Location }, cancellationToken);

    var ride = await mediator.Send(new GetActiveRide { DriverId = request.DriverId }, cancellationToken);
    if (ride == null)
      return;

    // Ride owns its own row, so it has to be told: the driver's position was previously applied
    // to the Driver aggregate only, leaving Ride.LastLocation — the field GET /rides/{id}/location
    // reads — frozen at the pickup point for the whole trip.
    await mediator.Send(new TrackRideLocation { RideId = ride.Id, Location = request.Location }, cancellationToken);

    var group = HubGroups.ForRide(ride.Id);

    await hub.Clients.Group(group).SendToRiderNewDriverLocation(request.Location);

    var eta = await mediator.Send(new GetRideETA { Id = ride.Id }, cancellationToken);

    await hub.Clients.Group(group).SendToRiderNewETA(eta.EstimatedArrivalMinutes, eta.DistanceKm);

    // Only while the driver is still on their way. Once the trip starts, PickupLocation holds
    // wherever the driver was at Start, so this distance measures how far they have driven —
    // under the threshold for the first several hundred metres, which re-fired "driver has
    // arrived" over and over mid-trip.
    if (ride.HasStarted)
      return;

    var distance = DistanceInMeters(ride.PickupLocation, request.Location);
    if (distance < configuration.GetValue<double>("Hub:ArrivalThresholdMeters"))
      await hub.Clients.Group(group).SendToRiderDriverArrival(ride.Id);
  }

  // NetTopologySuite's Point.Distance() is planar/Cartesian on the raw coordinate values (SRID
  // is metadata only) — on lat/lon points that returns degrees, not meters. Haversine gives the
  // real great-circle distance so it's comparable against ArrivalThresholdMeters.
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
