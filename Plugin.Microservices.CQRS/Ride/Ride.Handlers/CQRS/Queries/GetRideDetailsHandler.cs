using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Exceptions;
using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Ride.Core.CQRS.Queries;
using Ride.Core.Dtos;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Queries;

public class GetRideDetailsHandler(IRideContext db, RideMapper mapper) : IRequestHandler<GetRideDetails, RideDetailsResponse>
{
  public async Task<RideDetailsResponse> Handle(GetRideDetails request, CancellationToken cancellationToken)
  {
    var ride = await mapper.ProjectToRideDetails(db.Rides.AsNoTracking().Where(f => f.Id == request.Id))
      .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("ride_not_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    return ride;
  }
}
