using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ride.Api.Persistence;
using Ride.Api.Shared;
using Voyager.Contracts.Driver;

namespace Ride.Api.Features.GetRideETA;

/// <summary>No IDriverLocationQuery port — asks Driver for the driver's location via IMediator.Send directly; Arbitrer resolves it remotely.</summary>
public class GetRideETAHandler(RideDbContext db, IMediator mediator, IOptions<EtaConfig> config) : IRequestHandler<GetRideETA, ETAResponse>
{
  public async Task<ETAResponse> Handle(GetRideETA request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
      ?? throw new KeyNotFoundException("ride_not_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    var driverLocation = await mediator.Send(new GetDriverLocation { DriverId = ride.DriverId }, cancellationToken);
    var location = driverLocation?.LastLocation;

    if (ride.PickupLocation == null || location == null)
      return new ETAResponse();

    var distanceInMeters = RideEtaCalculator.DistanceInMeters(ride.PickupLocation, location);
    var (minutes, distanceKm) = RideEtaCalculator.Calculate(distanceInMeters, config.Value);

    return new ETAResponse { EstimatedArrivalMinutes = minutes, DistanceKm = distanceKm };
  }
}
