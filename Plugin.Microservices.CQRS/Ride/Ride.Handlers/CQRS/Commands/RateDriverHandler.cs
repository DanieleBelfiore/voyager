using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Exceptions;
using Identity.Core.CQRS.Commands;
using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Events;
using Ride.Core.Enums;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Commands;

public class RateDriverHandler(IRideContext db, IHikyaku mediator) : IRequestHandler<RateDriver>
{
  // The value feeds Identity's running average, so an out-of-range or replayed rating
  // permanently skews the target's score and the driver-matching rank built on top of it.
  private const int MinRating = 1;
  private const int MaxRating = 5;

  public async Task Handle(RateDriver request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.FirstOrDefaultAsync(f => f.Id == request.RideId, cancellationToken) ?? throw new NotFoundException("ride_not_found");

    if (ride.UserId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    if (ride.Status != RideStatus.Completed)
      throw new ConflictException("operation_not_permitted");

    if (request.Rating is < MinRating or > MaxRating)
      throw new InvalidInputException("rating_out_of_range");

    if (ride.DriverRating.HasValue)
      throw new ConflictException("ride_already_rated");

    ride.DriverRating = request.Rating;
    ride.LastUpdateDate = DateTime.UtcNow;

    // Persist the marker before telling Identity. If this order were reversed, a failure in
    // between would leave the rating counted upstream but not recorded here, so a retry would
    // count it twice and permanently skew the average.
    await db.SaveChangesAsync(cancellationToken);

    await mediator.Send(new UpdateUserRating { UserId = ride.DriverId, Rating = request.Rating }, cancellationToken);

    await mediator.Publish(new DriverRatingReceived { RideId = ride.Id, Rating = request.Rating }, cancellationToken);
  }
}
