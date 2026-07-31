using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using MediatR;

namespace Ride.Application.CQRS.Commands;

public class RateRideHandler(IRideRepository repository, IRatingUpdateService ratings, IRideEventPublisher events) : IRequestHandler<RateRide>
{
  public async Task Handle(RateRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdReadOnlyAsync(request.RideId, cancellationToken) ?? throw new Exception("ride_not_found");

    if (ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    await ratings.UpdateRatingAsync(ride.UserId, request.Rating, cancellationToken);

    await events.RiderRatingReceivedAsync(ride.Id, request.Rating, cancellationToken);
  }
}
