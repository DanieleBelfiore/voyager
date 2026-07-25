using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ride.Module.Persistence;
using Ride.Module.Shared;
using Voyager.Contracts.Driver;
using RideETAInfo = Voyager.Contracts.Ride.RideETAInfo;
using SharedGetRideETA = Voyager.Contracts.Ride.GetRideETA;

namespace Ride.Module.Features.GetRideETAForHub;

internal class GetRideETAForHubHandler(RideDbContext db, IMediator mediator, IOptions<EtaConfig> config) : IRequestHandler<SharedGetRideETA, RideETAInfo>
{
  public async Task<RideETAInfo> Handle(SharedGetRideETA request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
      ?? throw new Exception("ride_not_found");

    var driverLocation = await mediator.Send(new GetDriverLocation { DriverId = ride.DriverId }, cancellationToken);
    var location = driverLocation?.LastLocation;

    if (ride.PickupLocation == null || location == null)
      return new RideETAInfo();

    var distanceInMeters = ride.PickupLocation.Distance(location);
    var (minutes, distanceKm) = RideEtaCalculator.Calculate(distanceInMeters, config.Value.AverageSpeedKmh);

    return new RideETAInfo { EstimatedArrivalMinutes = minutes, DistanceKm = distanceKm };
  }
}
