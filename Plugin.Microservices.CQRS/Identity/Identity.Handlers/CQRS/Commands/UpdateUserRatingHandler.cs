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
    var user = await db.Users.Where(f => f.Id == request.UserId).FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("user_not_found");

    user.RatingsCount++;
    user.Ratings = user.RatingsCount <= 1 ? request.Rating : (user.Ratings * (user.RatingsCount - 1) + request.Rating) / (double)user.RatingsCount;

    await db.SaveChangesAsync(cancellationToken);

    return user.Ratings;
  }
}
