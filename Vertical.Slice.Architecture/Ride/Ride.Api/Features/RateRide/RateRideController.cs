using System;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Ride.Api.Features.RateRide;

[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class RateRideController(IHikyaku mediator) : ControllerBase
{
  [HttpPut("{rideId:guid}/rate")]
  public async Task<ActionResult> Rate(Guid rideId, [FromBody] RateRideRequest request, CancellationToken cancellationToken)
  {
    await mediator.Send(new RateRide { RideId = rideId, Rating = request.Rating, CallerId = this.GetUserId() }, cancellationToken);

    return Ok();
  }
}
