using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ride.Module.Features.StartRide;

[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class StartRideController(IMediator mediator) : ControllerBase
{
  [HttpPut("{rideId:guid}/start")]
  public async Task<ActionResult> Start(Guid rideId, [FromBody] StartRideRequest request, CancellationToken cancellationToken)
  {
    await mediator.Send(new StartRide { Id = rideId, Location = request.Location }, cancellationToken);

    return Ok();
  }
}
