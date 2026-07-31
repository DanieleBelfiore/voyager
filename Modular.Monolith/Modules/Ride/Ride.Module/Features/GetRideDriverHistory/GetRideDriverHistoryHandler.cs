using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Module.Entities;
using Ride.Module.Persistence;
using Ride.Module.Shared;

namespace Ride.Module.Features.GetRideDriverHistory;

internal class GetRideDriverHistoryHandler(RideDbContext db) : IRequestHandler<GetRideDriverHistory, List<RideDetailsResponse>>
{
  public async Task<List<RideDetailsResponse>> Handle(GetRideDriverHistory request, CancellationToken cancellationToken)
  {
    var take = request.Take <= 0 ? 25 : Math.Min(request.Take, 100);
    var page = Math.Max(request.Page, 0); // a negative page would otherwise produce a negative Skip() and throw

    var query = db.Rides.AsNoTracking().Where(r => r.DriverId == request.DriverId && r.Status == RideStatus.Completed)
      .OrderByDescending(r => r.RequestedAt)
      .Skip(take * page)
      .Take(take);

    var rides = await query.ToListAsync(cancellationToken);

    return rides.Select(RideDetailsResponse.From).ToList();
  }
}
