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

namespace Ride.Api.Features.GetRideETA;

/// <summary>No IDriverLocationQuery port — asks Driver for the driver's location via IHikyaku.Send directly; Kaido resolves it remotely.</summary>
public class GetRideETAHandler(RideDbContext db, IHikyaku mediator, IOptions<EtaConfig> config) : IRequestHandler<GetRideETA, ETAResponse>
{
  public async Task<ETAResponse> Handle(GetRideETA request, CancellationToken cancellationToken)
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
      return new ETAResponse();

    var distanceInMeters = RideEtaCalculator.DistanceInMeters(target, location);
    var (minutes, distanceKm) = RideEtaCalculator.Calculate(distanceInMeters, config.Value);

    return new ETAResponse { EstimatedArrivalMinutes = minutes, DistanceKm = distanceKm };
  }
}
