using System;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Ride.Module.Shared;
using Voyager.Shared.Extensions;

namespace Ride.Module.Features.GetRideDetails;

[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class GetRideDetailsController(IHikyaku mediator) : ControllerBase
{
  [HttpGet("{rideId:guid}")]
  public async Task<ActionResult<RideDetailsResponse>> Get(Guid rideId, CancellationToken cancellationToken)
  {
    return Ok(await mediator.Send(new GetRideDetails { Id = rideId, CallerId = this.GetUserId() }, cancellationToken));
  }
}
