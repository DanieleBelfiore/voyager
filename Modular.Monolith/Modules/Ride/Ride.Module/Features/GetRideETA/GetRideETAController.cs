using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Ride.Module.Features.GetRideETA;

[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class GetRideETAController(IMediator mediator) : ControllerBase
{
  [HttpGet("{rideId:guid}/eta")]
  public async Task<ActionResult<ETAResponse>> Get(Guid rideId, CancellationToken cancellationToken)
  {
    return Ok(await mediator.Send(new GetRideETA { Id = rideId, CallerId = this.GetUserId() }, cancellationToken));
  }
}
