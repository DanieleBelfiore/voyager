using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Ride.Api.Features.AcceptRide;

[Authorize(Policy = "RequireDriver")]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class AcceptRideController(IMediator mediator) : ControllerBase
{
  [HttpPut("{rideId:guid}/accept")]
  public async Task<ActionResult> Accept(Guid rideId, CancellationToken cancellationToken)
  {
    await mediator.Send(new AcceptRide { DriverId = this.GetUserId(), RideId = rideId }, cancellationToken);

    return Ok();
  }
}
