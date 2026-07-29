using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Driver.Api.Features.SearchBestDriver;

[Authorize]
[EnableRateLimiting("driver_api")]
[Route("api/v1/drivers")]
public class SearchBestDriverController(IMediator mediator) : ControllerBase
{
  [EnableRateLimiting("driver_search")]
  [HttpPost("search")]
  public async Task<ActionResult<List<SearchBestDriverResponse>>> Search([FromBody] SearchBestDriverRequest request, CancellationToken cancellationToken)
  {
    return Ok(await mediator.Send(new SearchBestDriver { UserId = this.GetUserId(), Location = request.Location, DistanceThresholdInMeters = request.DistanceThresholdInKm }, cancellationToken));
  }
}
