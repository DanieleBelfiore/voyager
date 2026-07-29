using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Module.Entities;
using Ride.Module.Persistence;
using Ride.Module.Shared;

namespace Ride.Module.Features.GetRideHistory;

internal class GetRideHistoryHandler(RideDbContext db) : IRequestHandler<GetRideHistory, List<RideDetailsResponse>>
{
  public async Task<List<RideDetailsResponse>> Handle(GetRideHistory request, CancellationToken cancellationToken)
  {
    var query = db.Rides.AsNoTracking().Where(r => r.UserId == request.UserId && r.Status == RideStatus.Completed)
      .OrderByDescending(r => r.RequestedAt)
      .Skip(request.Take * request.Page);

    var rides = await (request.Take < 0 ? query : query.Take(request.Take)).ToListAsync(cancellationToken);

    return rides.Select(RideDetailsResponse.From).ToList();
  }
}
