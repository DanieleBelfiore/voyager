using System;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Ride.Api.Features.GetRideCurrentLocation;

[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class GetRideCurrentLocationController(IHikyaku mediator) : ControllerBase
{
  [EnableRateLimiting("ride_location")]
  [HttpGet("{rideId:guid}/location")]
  public async Task<ActionResult<RideCurrentLocationResponse>> Get(Guid rideId, CancellationToken cancellationToken)
  {
    return Ok(await mediator.Send(new GetRideCurrentLocation { Id = rideId, CallerId = this.GetUserId() }, cancellationToken));
  }
}
