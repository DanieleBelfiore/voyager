using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Api.Entities;
using Ride.Api.Persistence;
using Ride.Api.Shared;

namespace Ride.Api.Features.GetRideHistory;

public class GetRideHistoryHandler(RideDbContext db) : IRequestHandler<GetRideHistory, List<RideDetailsResponse>>
{
  public async Task<List<RideDetailsResponse>> Handle(GetRideHistory request, CancellationToken cancellationToken)
  {
    var take = request.Take <= 0 ? 25 : Math.Min(request.Take, 100);
    var page = Math.Max(request.Page, 0); // a negative page would otherwise produce a negative Skip() and throw

    var query = db.Rides.AsNoTracking().Where(r => r.UserId == request.UserId && r.Status == RideStatus.Completed)
      .OrderByDescending(r => r.RequestedAt)
      .Skip(take * page)
      .Take(take);

    var rides = await query.ToListAsync(cancellationToken);

    return rides.Select(RideDetailsResponse.From).ToList();
  }
}
