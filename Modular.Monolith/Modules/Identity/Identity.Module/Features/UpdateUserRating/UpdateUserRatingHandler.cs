using System;
using System.Collections.Generic;
using System.Linq;
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
    // One atomic statement, computed from the row's own current values. The previous
    // load-mutate-save let two ratings landing together both read the same RatingsCount, so the
    // second SaveChanges silently overwrote the first and a rating vanished from the average
    // SearchBestDriver ranks on.
    // Captured rather than inlined so it is sent as a parameter, not a provider-specific
    // server-clock function.
    var now = DateTime.UtcNow;

    var affected = await db.Users
      .Where(u => u.Id == request.UserId)
      .ExecuteUpdateAsync(setters => setters
        .SetProperty(u => u.Ratings, u => (u.Ratings * u.RatingsCount + request.Rating) / (u.RatingsCount + 1))
        .SetProperty(u => u.RatingsCount, u => u.RatingsCount + 1)
        .SetProperty(u => u.Modified, _ => now), cancellationToken);

    if (affected == 0)
      throw new KeyNotFoundException("user_not_found");

    // Read back only for the caller's return value — the average is already committed.
    return await db.Users.AsNoTracking().Where(u => u.Id == request.UserId)
      .Select(u => u.Ratings)
      .FirstOrDefaultAsync(cancellationToken);
  }
}
