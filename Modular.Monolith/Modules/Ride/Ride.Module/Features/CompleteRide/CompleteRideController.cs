using System;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Ride.Module.Features.CompleteRide;

[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class CompleteRideController(IHikyaku mediator) : ControllerBase
{
  [HttpPut("{rideId:guid}/complete")]
  public async Task<ActionResult> Complete(Guid rideId, [FromBody] CompleteRideRequest request, CancellationToken cancellationToken)
  {
    await mediator.Send(new CompleteRide { Id = rideId, Location = request.Location, CallerId = this.GetUserId() }, cancellationToken);

    return Ok();
  }
}
