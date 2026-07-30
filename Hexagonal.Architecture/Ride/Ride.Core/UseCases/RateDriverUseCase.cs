using System;
using System.Linq;
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

    var driverRides = await repository.GetDriverHistoryAsync(ride.DriverId, -1, 0, cancellationToken);

    await ratings.UpdateRatingAsync(ride.DriverId, request.Rating, driverRides.Count, cancellationToken);

    var latestRide = driverRides.FirstOrDefault();
    if (latestRide == null)
      return;

    await events.DriverRatingReceivedAsync(latestRide.Id, request.Rating, cancellationToken);
  }
}
