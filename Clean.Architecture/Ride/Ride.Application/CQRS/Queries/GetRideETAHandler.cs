using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Dtos;
using Ride.Application.Ports;
using MediatR;

namespace Ride.Application.CQRS.Queries;

public class GetRideETAHandler(IRideRepository repository, IDriverLocationQuery driverLocation, IEtaConfig config) : IRequestHandler<GetRideETA, ETAResponse>
{
  public async Task<ETAResponse> Handle(GetRideETA request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdReadOnlyAsync(request.Id, cancellationToken) ?? throw new Exception("ride_not_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    var location = await driverLocation.GetLocationAsync(ride.DriverId, cancellationToken);

    if (ride.PickupLocation == null || location == null)
      return new ETAResponse();

    var distanceInMeters = ride.PickupLocation.Distance(location);
    var (minutes, distanceKm) = RideEtaCalculator.Calculate(distanceInMeters, config.AverageSpeedKmh);

    return new ETAResponse { EstimatedArrivalMinutes = minutes, DistanceKm = distanceKm };
  }
}
