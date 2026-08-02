using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Exceptions;
using Identity.Core.CQRS.Commands;
using Identity.Handlers.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Handlers.CQRS.Commands;

public class UpdateUserRatingHandler(IIdentityContext db) : IRequestHandler<UpdateUserRating, double>
{
  // RatingsCount is self-tracked here (incremented once per call) rather than passed in by the
  // caller — the caller previously supplied "rides completed", which isn't the same number as
  // "ratings actually received" (a completed ride isn't necessarily rated).
  public async Task<double> Handle(UpdateUserRating request, CancellationToken cancellationToken)
  {
    // One atomic statement, computed from the row's own current values. The previous
    // load-mutate-save let two ratings landing together both read the same RatingsCount, so the
    // second SaveChanges silently overwrote the first and a rating vanished from the average
    // SearchBestDriver ranks on.
    // Captured rather than inlined so it is sent as a parameter, not a provider-specific
    // server-clock function.
    var now = DateTime.UtcNow;

    var affected = await db.Users
      .Where(f => f.Id == request.UserId)
      .ExecuteUpdateAsync(setters => setters
        .SetProperty(f => f.Ratings, f => (f.Ratings * f.RatingsCount + request.Rating) / (f.RatingsCount + 1))
        .SetProperty(f => f.RatingsCount, f => f.RatingsCount + 1)
        .SetProperty(f => f.Modified, _ => now), cancellationToken);

    if (affected == 0)
      throw new NotFoundException("user_not_found");

    // Read back only for the caller's return value — the average is already committed.
    return await db.Users.AsNoTracking().Where(f => f.Id == request.UserId)
      .Select(f => f.Ratings)
      .FirstOrDefaultAsync(cancellationToken);
  }
}
