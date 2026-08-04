using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using Hikyaku;
using Voyager.Contracts.Identity;

namespace Ride.Infrastructure.Messaging;

public class RemoteRatingUpdateService(IHikyaku mediator) : IRatingUpdateService
{
  public async Task<double> UpdateRatingAsync(Guid userId, int rating, CancellationToken cancellationToken)
  {
    return await mediator.Send(new UpdateUserRating { UserId = userId, Rating = rating }, cancellationToken);
  }
}
