using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Ride.Api.Shared;
using Voyager.Shared.Extensions;

namespace Ride.Api.Features.GetRideHistory;

[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class GetRideHistoryController(IMediator mediator) : ControllerBase
{
  [HttpGet("history")]
  public async Task<ActionResult<List<RideDetailsResponse>>> Get(CancellationToken cancellationToken, int take = 25, int page = 0)
  {
    return Ok(await mediator.Send(new GetRideHistory { UserId = this.GetUserId(), Take = take, Page = page }, cancellationToken));
  }
}
