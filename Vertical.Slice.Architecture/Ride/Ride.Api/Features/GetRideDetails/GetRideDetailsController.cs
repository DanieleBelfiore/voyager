using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Ride.Api.Shared;

namespace Ride.Api.Features.GetRideDetails;

[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class GetRideDetailsController(IMediator mediator) : ControllerBase
{
  [HttpGet("{rideId:guid}")]
  public async Task<ActionResult<RideDetailsResponse>> Get(Guid rideId, CancellationToken cancellationToken)
  {
    return Ok(await mediator.Send(new GetRideDetails { Id = rideId }, cancellationToken));
  }
}
