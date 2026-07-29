using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using MediatR;

namespace Ride.Application.CQRS.Commands;

public class RateDriverHandler(IRideRepository repository, IRatingUpdateService ratings, IRideEventPublisher events) : IRequestHandler<RateDriver>
{
  public async Task Handle(RateDriver request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdReadOnlyAsync(request.RideId, cancellationToken) ?? throw new Exception("ride_not_found");

    var driverRides = await repository.GetDriverHistoryAsync(ride.DriverId, -1, 0, cancellationToken);

    await ratings.UpdateRatingAsync(ride.DriverId, request.Rating, driverRides.Count, cancellationToken);

    var latestRide = driverRides.FirstOrDefault();
    if (latestRide == null)
      return;

    await events.DriverRatingReceivedAsync(latestRide.Id, request.Rating, cancellationToken);
  }
}
