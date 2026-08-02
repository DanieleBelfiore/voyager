using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Exceptions;
using MediatR;
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

    await db.SaveChangesAsync(cancellationToken);

    await mediator.Publish(new NewRideRequested { RideId = ride.Id, DriverId = ride.DriverId }, cancellationToken);

    return mapper.ToRideDetails(ride);
  }
}
