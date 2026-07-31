using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Ride.Core.Ports.Secondary;
using RideETAInfo = Voyager.Contracts.Ride.RideETAInfo;
using SharedGetRideETA = Voyager.Contracts.Ride.GetRideETA;

namespace Ride.Core.UseCases;

/// <summary>Remote-only handler — see GetActiveRideForHubUseCase for why there's no primary port.</summary>
public class GetRideETAForHubUseCase(IRideRepository repository, IDriverLocationQuery driverLocation, IEtaConfig config) : IRequestHandler<SharedGetRideETA, RideETAInfo>
{
  public async Task<RideETAInfo> Handle(SharedGetRideETA request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdReadOnlyAsync(request.Id, cancellationToken) ?? throw new Exception("ride_not_found");

    var location = await driverLocation.GetLocationAsync(ride.DriverId, cancellationToken);

    if (ride.PickupLocation == null || location == null)
      return new RideETAInfo();

    var distanceInMeters = RideEtaCalculator.DistanceInMeters(ride.PickupLocation, location);
    var (minutes, distanceKm) = RideEtaCalculator.Calculate(distanceInMeters, config);

    return new RideETAInfo { EstimatedArrivalMinutes = minutes, DistanceKm = distanceKm };
  }
}
