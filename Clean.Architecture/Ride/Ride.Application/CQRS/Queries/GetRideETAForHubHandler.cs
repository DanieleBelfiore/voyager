using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using Hikyaku;
using RideETAInfo = Voyager.Contracts.Ride.RideETAInfo;
using SharedGetRideETA = Voyager.Contracts.Ride.GetRideETA;

namespace Ride.Application.CQRS.Queries;

/// <summary>
/// Handles the shared Voyager.Contracts.Ride.GetRideETA contract directly — the remote target
/// Hub's VoyagerHub.UpdateDriverLocation dispatches to via Kaido.
/// </summary>
public class GetRideETAForHubHandler(IRideRepository repository, IDriverLocationQuery driverLocation, IEtaConfig config) : IRequestHandler<SharedGetRideETA, RideETAInfo>
{
  public async Task<RideETAInfo> Handle(SharedGetRideETA request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdReadOnlyAsync(request.Id, cancellationToken) ?? throw new KeyNotFoundException("ride_not_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    var location = await driverLocation.GetLocationAsync(ride.DriverId, cancellationToken);

    // Once the trip is under way the driver is heading for the dropoff, not the pickup, so the
    // endpoint has to switch — measuring against the pickup from here on reports distance already
    // travelled instead of distance still to go.
    var target = ride.HasStarted() ? ride.DropoffLocation : ride.PickupLocation;

    if (target == null || location == null)
      return new RideETAInfo();

    var distanceInMeters = RideEtaCalculator.DistanceInMeters(target, location);
    var (minutes, distanceKm) = RideEtaCalculator.Calculate(distanceInMeters, config);

    return new RideETAInfo { EstimatedArrivalMinutes = minutes, DistanceKm = distanceKm };
  }
}
