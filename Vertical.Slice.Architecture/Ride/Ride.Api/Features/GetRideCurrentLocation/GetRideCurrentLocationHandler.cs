using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Api.Persistence;

namespace Ride.Api.Features.GetRideCurrentLocation;

public class GetRideCurrentLocationHandler(RideDbContext db) : IRequestHandler<GetRideCurrentLocation, RideCurrentLocationResponse>
{
  public async Task<RideCurrentLocationResponse> Handle(GetRideCurrentLocation request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
      ?? throw new Exception("ride_not_found");

    return new RideCurrentLocationResponse { LastLocation = ride.LastLocation, LastUpdateDate = ride.LastUpdateDate };
  }
}
