using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ride.Api.Persistence;
using Ride.Api.Shared;
using Voyager.Contracts.Driver;
using RideETAInfo = Voyager.Contracts.Ride.RideETAInfo;
using SharedGetRideETA = Voyager.Contracts.Ride.GetRideETA;

namespace Ride.Api.Features.GetRideETAForHub;

/// <summary>Remote-only handler — see GetActiveRideForHub for why there's no local endpoint.</summary>
public class GetRideETAForHubHandler(RideDbContext db, IHikyaku mediator, IOptions<EtaConfig> config) : IRequestHandler<SharedGetRideETA, RideETAInfo>
{
  public async Task<RideETAInfo> Handle(SharedGetRideETA request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
      ?? throw new KeyNotFoundException("ride_not_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    var driverLocation = await mediator.Send(new GetDriverLocation { DriverId = ride.DriverId }, cancellationToken);
    var location = driverLocation?.LastLocation;

    // Once the trip is under way the driver is heading for the dropoff, not the pickup, so the
    // endpoint has to switch — measuring against the pickup from here on reports distance already
    // travelled instead of distance still to go.
    var target = ride.HasStarted() ? ride.DropoffLocation : ride.PickupLocation;

    if (target == null || location == null)
      return new RideETAInfo();

    var distanceInMeters = RideEtaCalculator.DistanceInMeters(target, location);
    var (minutes, distanceKm) = RideEtaCalculator.Calculate(distanceInMeters, config.Value);

    return new RideETAInfo { EstimatedArrivalMinutes = minutes, DistanceKm = distanceKm };
  }
}
