using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Identity.Core.CQRS.Commands;
using Identity.Handlers.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ride.Handlers.CQRS.Queries
{
  public class UpdateUserRatingHandler(IIdentityContext db) : IRequestHandler<UpdateUserRating, double>
  {
    public async Task<double> Handle(UpdateUserRating request, CancellationToken cancellationToken)
    {
      var user = await db.Users.Where(f => f.Id == request.UserId).FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("user_not_found");

      var newRating = (user.Ratings + request.Rating) / request.Rides;

      user.Ratings = newRating;

      await db.SaveChangesAsync(cancellationToken);

      return newRating;
    }
  }
}
