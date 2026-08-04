using System;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Ride.Module.Features.CancelRide;

[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class CancelRideController(IHikyaku mediator) : ControllerBase
{
  [EnableRateLimiting("ride_cancellation")]
  [HttpPut("{rideId:guid}/cancel")]
  public async Task<ActionResult> Cancel(Guid rideId, [FromBody] CancelRideRequest request, CancellationToken cancellationToken)
  {
    await mediator.Send(new CancelRide { Id = rideId, CancellationReason = request.CancellationReason, CallerId = this.GetUserId() }, cancellationToken);

    return Ok();
  }
}
