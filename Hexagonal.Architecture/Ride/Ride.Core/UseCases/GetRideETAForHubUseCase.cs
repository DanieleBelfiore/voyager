using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Ride.Core.Ports.Secondary;
using RideETAInfo = Voyager.Contracts.Ride.RideETAInfo;
using SharedGetRideETA = Voyager.Contracts.Ride.GetRideETA;

namespace Ride.Core.UseCases;

/// <summary>Remote-only handler — see GetActiveRideForHubUseCase for why there's no primary port.</summary>
public class GetRideETAForHubUseCase(IRideRepository repository, IDriverLocationQuery driverLocation, IEtaConfig config) : IRequestHandler<SharedGetRideETA, RideETAInfo>
{
  public async Task<RideETAInfo> Handle(SharedGetRideETA request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdReadOnlyAsync(request.Id, cancellationToken) ?? throw new KeyNotFoundException("ride_not_found");

    var location = await driverLocation.GetLocationAsync(ride.DriverId, cancellationToken);

    // Once the trip is under way the driver is not heading to the pickup any more — and Start
    // overwrote PickupLocation with the driver's own position at that moment, so measuring
    // against it reports distance already travelled instead of distance still to go.
    var target = ride.HasStarted() ? ride.DropoffLocation : ride.PickupLocation;

    if (target == null || location == null)
      return new RideETAInfo();

    var distanceInMeters = RideEtaCalculator.DistanceInMeters(target, location);
    var (minutes, distanceKm) = RideEtaCalculator.Calculate(distanceInMeters, config);

    return new RideETAInfo { EstimatedArrivalMinutes = minutes, DistanceKm = distanceKm };
  }
}
