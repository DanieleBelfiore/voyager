using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class RateRideUseCase(IRideRepository repository, IRatingUpdateService ratings, IRideEventPublisher events) : IRateRideUseCase
{
  public async Task Handle(RateRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdAsync(request.RideId, cancellationToken) ?? throw new KeyNotFoundException("ride_not_found");

    if (ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    ride.RateRider(request.Rating);

    // Persist the marker before telling Identity. If this order were reversed, a failure in
    // between would leave the rating counted upstream but not recorded here, so a retry would
    // count it twice and permanently skew the average — losing a rating is recoverable, double
    // counting is not.
    await repository.SaveChangesAsync(cancellationToken);

    await ratings.UpdateRatingAsync(ride.UserId, request.Rating, cancellationToken);

    await events.RiderRatingReceivedAsync(ride.Id, request.Rating, cancellationToken);
  }
}
