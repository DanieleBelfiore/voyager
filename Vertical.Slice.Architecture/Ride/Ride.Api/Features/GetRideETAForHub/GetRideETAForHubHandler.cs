using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ride.Api.Persistence;
using Ride.Api.Shared;
using Voyager.Contracts.Driver;
using RideETAInfo = Voyager.Contracts.Ride.RideETAInfo;
using SharedGetRideETA = Voyager.Contracts.Ride.GetRideETA;

namespace Ride.Api.Features.GetRideETAForHub;

/// <summary>Remote-only handler — see GetActiveRideForHub for why there's no local endpoint.</summary>
public class GetRideETAForHubHandler(RideDbContext db, IMediator mediator, IOptions<EtaConfig> config) : IRequestHandler<SharedGetRideETA, RideETAInfo>
{
  public async Task<RideETAInfo> Handle(SharedGetRideETA request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
      ?? throw new Exception("ride_not_found");

    var driverLocation = await mediator.Send(new GetDriverLocation { DriverId = ride.DriverId }, cancellationToken);
    var location = driverLocation?.LastLocation;

    if (ride.PickupLocation == null || location == null)
      return new RideETAInfo();

    var distanceInMeters = RideEtaCalculator.DistanceInMeters(ride.PickupLocation, location);
    var (minutes, distanceKm) = RideEtaCalculator.Calculate(distanceInMeters, config.Value);

    return new RideETAInfo { EstimatedArrivalMinutes = minutes, DistanceKm = distanceKm };
  }
}
