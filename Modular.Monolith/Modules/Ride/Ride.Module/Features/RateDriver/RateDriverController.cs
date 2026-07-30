using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Ride.Module.Features.RateDriver;

[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class RateDriverController(IMediator mediator) : ControllerBase
{
  [HttpPut("{rideId:guid}/rate/driver")]
  public async Task<ActionResult> Rate(Guid rideId, [FromBody] RateDriverRequest request, CancellationToken cancellationToken)
  {
    await mediator.Send(new RateDriver { RideId = rideId, Rating = request.Rating, CallerId = this.GetUserId() }, cancellationToken);

    return Ok();
  }
}
