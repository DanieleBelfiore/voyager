using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Api.Persistence;
using Ride.Api.Shared;

namespace Ride.Api.Features.GetRideDetails;

public class GetRideDetailsHandler(RideDbContext db) : IRequestHandler<GetRideDetails, RideDetailsResponse>
{
  public async Task<RideDetailsResponse> Handle(GetRideDetails request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
      ?? throw new Exception("ride_not_found");

    return RideDetailsResponse.From(ride);
  }
}
