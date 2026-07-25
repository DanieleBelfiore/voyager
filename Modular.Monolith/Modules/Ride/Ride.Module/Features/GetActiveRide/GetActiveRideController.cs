using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Ride.Module.Features.GetActiveRide;

[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class GetActiveRideController(IMediator mediator) : ControllerBase
{
  [HttpGet("active")]
  public async Task<ActionResult<ActiveRideResponse>> Get(CancellationToken cancellationToken)
  {
    return Ok(await mediator.Send(new GetActiveRide { UserId = this.GetUserId() }, cancellationToken));
  }
}
