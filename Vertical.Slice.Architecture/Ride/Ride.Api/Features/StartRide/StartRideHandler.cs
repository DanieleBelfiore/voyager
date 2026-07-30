using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Api.Persistence;

namespace Ride.Api.Features.StartRide;

public class StartRideHandler(RideDbContext db) : IRequestHandler<StartRide>
{
  public async Task Handle(StartRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken) ?? throw new Exception("no_ride_found");

    if (ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    ride.Start(request.Location);

    await db.SaveChangesAsync(cancellationToken);
  }
}
