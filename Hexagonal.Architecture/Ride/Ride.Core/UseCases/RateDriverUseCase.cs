using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class RateDriverUseCase(IRideRepository repository, IRatingUpdateService ratings, IRideEventPublisher events) : IRateDriverUseCase
{
  public async Task Handle(RateDriver request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdAsync(request.RideId, cancellationToken) ?? throw new KeyNotFoundException("ride_not_found");

    if (ride.UserId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    ride.RateDriver(request.Rating);

    // Persist the marker before telling Identity. If this order were reversed, a failure in
    // between would leave the rating counted upstream but not recorded here, so a retry would
    // count it twice and permanently skew the average — losing a rating is recoverable, double
    // counting is not.
    await repository.SaveChangesAsync(cancellationToken);

    await ratings.UpdateRatingAsync(ride.DriverId, request.Rating, cancellationToken);

    await events.DriverRatingReceivedAsync(ride.Id, request.Rating, cancellationToken);
  }
}
