using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Exceptions;
using Driver.Core.CQRS.Queries;
using Driver.Core.Enums;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Events;
using Ride.Core.Dtos;
using Ride.Core.Enums;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Commands;

public class RequestRideHandler(IRideContext db, RideMapper mapper, IMediator mediator) : IRequestHandler<RequestRide, RideDetailsResponse>
{
  public async Task<RideDetailsResponse> Handle(RequestRide request, CancellationToken cancellationToken)
  {
    // DriverId comes straight off the request body, so it is checked before a ride is created
    // against it — otherwise a rider can pin a ride onto any GUID at all, including one that
    // belongs to a non-driver or to a driver already committed to someone else's trip.
    var driver = await mediator.Send(new GetDriverStatus { Id = request.DriverId }, cancellationToken)
      ?? throw new NotFoundException("driver_not_found");

    if (driver.Status != DriverStatus.Available)
      throw new ConflictException("driver_not_available");

    var status = new List<RideStatus> { RideStatus.Requested, RideStatus.DriverAssigned, RideStatus.InProgress };

    var alreadyRequested = await db.Rides.AsNoTracking().AnyAsync(f => f.UserId == request.UserId && status.Contains(f.Status), cancellationToken);
    if (alreadyRequested)
      throw new ConflictException("ride_already_in_progress");

    var ride = new Models.Ride
    {
      UserId = request.UserId,
      DriverId = request.DriverId,
      Status = RideStatus.Requested,
      PickupLocation = request.PickupLocation,
      PickupLocationGeoJSON = new WKTWriter().Write(request.PickupLocation),
      DropoffLocation = request.DropoffLocation,
      DropoffLocationGeoJSON = new WKTWriter().Write(request.DropoffLocation)
    };

    db.Rides.Add(ride);

    try
    {
      await db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
    {
      // Two concurrent requests can both pass the AnyAsync check above; the unique filtered
      // index IX_Rides_UserId_ActiveOnly (see RideContext.OnModelCreating) is what actually
      // guarantees one active ride per user, and this turns losing that race into the same
      // conflict the in-memory check already reports.
      throw new ConflictException("ride_already_in_progress");
    }

    await mediator.Publish(new NewRideRequested { RideId = ride.Id, DriverId = ride.DriverId }, cancellationToken);

    return mapper.ToRideDetails(ride);
  }
}
