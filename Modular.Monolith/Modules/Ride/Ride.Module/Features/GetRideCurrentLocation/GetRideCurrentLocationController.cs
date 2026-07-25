using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ride.Module.Features.GetRideCurrentLocation;

[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class GetRideCurrentLocationController(IMediator mediator) : ControllerBase
{
  [EnableRateLimiting("ride_location")]
  [HttpGet("{rideId:guid}/location")]
  public async Task<ActionResult<RideCurrentLocationResponse>> Get(Guid rideId, CancellationToken cancellationToken)
  {
    return Ok(await mediator.Send(new GetRideCurrentLocation { Id = rideId }, cancellationToken));
  }
}
