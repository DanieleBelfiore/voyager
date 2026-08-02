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
    var ride = await repository.GetByIdReadOnlyAsync(request.RideId, cancellationToken) ?? throw new KeyNotFoundException("ride_not_found");

    if (ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    await ratings.UpdateRatingAsync(ride.UserId, request.Rating, cancellationToken);

    await events.RiderRatingReceivedAsync(ride.Id, request.Rating, cancellationToken);
  }
}
