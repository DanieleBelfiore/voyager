using System;
using System.Threading;
using System.Threading.Tasks;
using Identity.Api.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Features.UpdateUserRating;

/// <summary>
/// Handles the shared <see cref="Voyager.Contracts.Identity.UpdateUserRating"/> contract —
/// no local endpoint, this is only ever driven remotely by Ride via Arbitrer after a completed
/// ride is rated.
/// </summary>
public class UpdateUserRatingHandler(IdentityDbContext db) : IRequestHandler<Voyager.Contracts.Identity.UpdateUserRating, double>
{
  public async Task<double> Handle(Voyager.Contracts.Identity.UpdateUserRating request, CancellationToken cancellationToken)
  {
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
      ?? throw new InvalidOperationException("user_not_found");

    user.UpdateRating(request.Rating);

    await db.SaveChangesAsync(cancellationToken);

    return user.Ratings;
  }
}
