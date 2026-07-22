using System;
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
    var ride = await repository.GetByIdReadOnlyAsync(request.Id, cancellationToken) ?? throw new Exception("ride_not_found");

    var location = await driverLocation.GetLocationAsync(ride.DriverId, cancellationToken);

    if (ride.PickupLocation == null || location == null)
      return new ETAResponse();

    var distanceInMeters = ride.PickupLocation.Distance(location);
    var (minutes, distanceKm) = RideEtaCalculator.Calculate(distanceInMeters, config.AverageSpeedKmh);

    return new ETAResponse { EstimatedArrivalMinutes = minutes, DistanceKm = distanceKm };
  }
}
