using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Api.Entities;
using Ride.Api.Persistence;
using Voyager.Contracts.Identity;
using Voyager.Contracts.Ride;

namespace Ride.Api.Features.RateDriver;

public class RateDriverHandler(RideDbContext db, IMediator mediator) : IRequestHandler<RateDriver>
{
  public async Task Handle(RateDriver request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RideId, cancellationToken)
      ?? throw new Exception("ride_not_found");

    if (ride.UserId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    var driverRides = await db.Rides.AsNoTracking()
      .Where(r => r.DriverId == ride.DriverId && r.Status == RideStatus.Completed)
      .OrderByDescending(r => r.RequestedAt)
      .ToListAsync(cancellationToken);

    await mediator.Send(new UpdateUserRating { UserId = ride.DriverId, Rating = request.Rating, Rides = driverRides.Count }, cancellationToken);

    var latestRide = driverRides.FirstOrDefault();
    if (latestRide == null)
      return;

    await mediator.Publish(new DriverRatingReceived { RideId = latestRide.Id, Rating = request.Rating }, cancellationToken);
  }
}
