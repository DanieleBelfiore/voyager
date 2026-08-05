using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Dtos;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class GetRideETAUseCase(IRideRepository repository, IDriverLocationQuery driverLocation, IEtaConfig config) : IGetRideETAUseCase
{
  public async Task<ETAResponse> Handle(GetRideETA request, CancellationToken cancellationToken)
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
      return new ETAResponse();

    var distanceInMeters = RideEtaCalculator.DistanceInMeters(target, location);
    var (minutes, distanceKm) = RideEtaCalculator.Calculate(distanceInMeters, config);

    return new ETAResponse { EstimatedArrivalMinutes = minutes, DistanceKm = distanceKm };
  }
}
