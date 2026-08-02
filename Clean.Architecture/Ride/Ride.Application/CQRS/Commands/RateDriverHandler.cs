using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using MediatR;

namespace Ride.Application.CQRS.Commands;

public class RateDriverHandler(IRideRepository repository, IRatingUpdateService ratings, IRideEventPublisher events) : IRequestHandler<RateDriver>
{
  public async Task Handle(RateDriver request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdReadOnlyAsync(request.RideId, cancellationToken) ?? throw new KeyNotFoundException("ride_not_found");

    if (ride.UserId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    await ratings.UpdateRatingAsync(ride.DriverId, request.Rating, cancellationToken);

    await events.DriverRatingReceivedAsync(ride.Id, request.Rating, cancellationToken);
  }
}
