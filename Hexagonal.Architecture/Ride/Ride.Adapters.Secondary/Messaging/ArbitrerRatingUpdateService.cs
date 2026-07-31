using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Ports.Secondary;
using MediatR;
using Voyager.Contracts.Identity;

namespace Ride.Adapters.Secondary.Messaging;

public class ArbitrerRatingUpdateService(IMediator mediator) : IRatingUpdateService
{
  public async Task<double> UpdateRatingAsync(Guid userId, int rating, CancellationToken cancellationToken)
  {
    return await mediator.Send(new UpdateUserRating { UserId = userId, Rating = rating }, cancellationToken);
  }
}
