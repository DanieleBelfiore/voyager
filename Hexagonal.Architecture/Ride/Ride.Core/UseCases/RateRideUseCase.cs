using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class RateRideUseCase(IRideRepository repository, IRatingUpdateService ratings, IRideEventPublisher events) : IRateRideUseCase
{
  public async Task Handle(RateRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdReadOnlyAsync(request.RideId, cancellationToken) ?? throw new Exception("ride_not_found");

    var rideCount = await repository.GetUserHistoryAsync(ride.UserId, -1, 0, cancellationToken);

    await ratings.UpdateRatingAsync(ride.UserId, request.Rating, rideCount.Count, cancellationToken);

    await events.RiderRatingReceivedAsync(ride.Id, request.Rating, cancellationToken);
  }
}
