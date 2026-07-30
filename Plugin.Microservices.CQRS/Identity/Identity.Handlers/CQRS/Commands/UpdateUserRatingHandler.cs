using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Identity.Core.CQRS.Commands;
using Identity.Handlers.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Handlers.CQRS.Commands;

public class UpdateUserRatingHandler(IIdentityContext db) : IRequestHandler<UpdateUserRating, double>
{
  public async Task<double> Handle(UpdateUserRating request, CancellationToken cancellationToken)
  {
    var user = await db.Users.Where(f => f.Id == request.UserId).FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("user_not_found");

    var newRating = request.Rides <= 1 ? request.Rating : (user.Ratings * (request.Rides - 1) + request.Rating) / (double)request.Rides;

    user.Ratings = newRating;

    await db.SaveChangesAsync(cancellationToken);

    return newRating;
  }
}
