using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using MediatR;
using RideETAInfo = Voyager.Contracts.Ride.RideETAInfo;
using SharedGetRideETA = Voyager.Contracts.Ride.GetRideETA;

namespace Ride.Application.CQRS.Queries;

/// <summary>
/// Handles the shared Voyager.Contracts.Ride.GetRideETA contract directly — the remote target
/// Hub's VoyagerHub.UpdateDriverLocation dispatches to via Arbitrer.
/// </summary>
public class GetRideETAForHubHandler(IRideRepository repository, IDriverLocationQuery driverLocation, IEtaConfig config) : IRequestHandler<SharedGetRideETA, RideETAInfo>
{
  public async Task<RideETAInfo> Handle(SharedGetRideETA request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdReadOnlyAsync(request.Id, cancellationToken) ?? throw new Exception("ride_not_found");

    var location = await driverLocation.GetLocationAsync(ride.DriverId, cancellationToken);

    if (ride.PickupLocation == null || location == null)
      return new RideETAInfo();

    var distanceInMeters = ride.PickupLocation.Distance(location);
    var (minutes, distanceKm) = RideEtaCalculator.Calculate(distanceInMeters, config.AverageSpeedKmh);

    return new RideETAInfo { EstimatedArrivalMinutes = minutes, DistanceKm = distanceKm };
  }
}
