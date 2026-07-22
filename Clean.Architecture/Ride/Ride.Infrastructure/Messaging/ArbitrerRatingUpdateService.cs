using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using MediatR;
using Voyager.Contracts.Identity;

namespace Ride.Infrastructure.Messaging;

public class ArbitrerRatingUpdateService(IMediator mediator) : IRatingUpdateService
{
  public async Task<double> UpdateRatingAsync(Guid userId, int rating, int rides, CancellationToken cancellationToken)
  {
    return await mediator.Send(new UpdateUserRating { UserId = userId, Rating = rating, Rides = rides }, cancellationToken);
  }
}
