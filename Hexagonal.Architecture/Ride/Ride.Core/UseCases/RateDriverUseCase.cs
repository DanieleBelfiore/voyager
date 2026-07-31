using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class RateDriverUseCase(IRideRepository repository, IRatingUpdateService ratings, IRideEventPublisher events) : IRateDriverUseCase
{
  public async Task Handle(RateDriver request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdReadOnlyAsync(request.RideId, cancellationToken) ?? throw new Exception("ride_not_found");

    if (ride.UserId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    await ratings.UpdateRatingAsync(ride.DriverId, request.Rating, cancellationToken);

    await events.DriverRatingReceivedAsync(ride.Id, request.Rating, cancellationToken);
  }
}
