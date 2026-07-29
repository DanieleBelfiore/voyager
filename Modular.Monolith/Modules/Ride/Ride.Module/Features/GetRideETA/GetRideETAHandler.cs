using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ride.Module.Persistence;
using Ride.Module.Shared;
using Voyager.Contracts.Driver;

namespace Ride.Module.Features.GetRideETA;

internal class GetRideETAHandler(RideDbContext db, IMediator mediator, IOptions<EtaConfig> config) : IRequestHandler<GetRideETA, ETAResponse>
{
  public async Task<ETAResponse> Handle(GetRideETA request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
      ?? throw new Exception("ride_not_found");

    var driverLocation = await mediator.Send(new GetDriverLocation { DriverId = ride.DriverId }, cancellationToken);
    var location = driverLocation?.LastLocation;

    if (ride.PickupLocation == null || location == null)
      return new ETAResponse();

    var distanceInMeters = ride.PickupLocation.Distance(location);
    var (minutes, distanceKm) = RideEtaCalculator.Calculate(distanceInMeters, config.Value.AverageSpeedKmh);

    return new ETAResponse { EstimatedArrivalMinutes = minutes, DistanceKm = distanceKm };
  }
}
