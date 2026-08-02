using System;
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
  // Clamped the same way RideController clamps `take`: DistanceThresholdInKm is caller-supplied
  // and drives a spatial scan, so an absurd radius is capped rather than trusted. MaxCandidates
  // bounds the result set on top of this.
  private const int MaxSearchRadiusKm = 50;

  [EnableRateLimiting("driver_search")]
  [HttpPost("search")]
  public async Task<ActionResult<List<SearchBestDriverResponse>>> Search([FromBody] SearchBestDriverRequest request, CancellationToken cancellationToken)
  {
    // The request is in kilometres, the query is in metres: SearchBestDriverHandler compares
    // against a geography Distance(), which SQL Server evaluates as STDistance in metres.
    return Ok(await mediator.Send(new SearchBestDriver { UserId = this.GetUserId(), Location = request.Location, DistanceThresholdInMeters = Math.Clamp(request.DistanceThresholdInKm, 1, MaxSearchRadiusKm) * 1000 }, cancellationToken));
  }
}
