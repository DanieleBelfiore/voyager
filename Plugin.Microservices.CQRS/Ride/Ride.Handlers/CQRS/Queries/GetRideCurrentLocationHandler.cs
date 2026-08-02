using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Core.CQRS.Queries;
using Ride.Core.Dtos;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Queries;

public class GetRideCurrentLocationHandler(IRideContext db) : IRequestHandler<GetRideCurrentLocation, RideCurrentLocationResponse>
{
  public async Task<RideCurrentLocationResponse> Handle(GetRideCurrentLocation request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken) ?? throw new NotFoundException("ride_not_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    return new RideCurrentLocationResponse { LastLocation = ride.LastLocation, LastUpdateDate = ride.LastUpdateDate };
  }
}
