using System;
using System.Threading;
using System.Threading.Tasks;
using Identity.Module.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Module.Features.UpdateUserRating;

/// <summary>
/// Handles the shared Voyager.Contracts.Identity.UpdateUserRating contract — no controller,
/// only ever driven by Ride's module calling IMediator.Send in the same process.
/// </summary>
internal class UpdateUserRatingHandler(IdentityDbContext db) : IRequestHandler<Voyager.Contracts.Identity.UpdateUserRating, double>
{
  public async Task<double> Handle(Voyager.Contracts.Identity.UpdateUserRating request, CancellationToken cancellationToken)
  {
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
      ?? throw new InvalidOperationException("user_not_found");

    user.UpdateRating(request.Rating, request.Rides);

    await db.SaveChangesAsync(cancellationToken);

    return user.Ratings;
  }
}
