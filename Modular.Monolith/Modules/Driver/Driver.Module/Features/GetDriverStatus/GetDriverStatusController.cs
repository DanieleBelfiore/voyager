using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Driver.Module.Features.GetDriverStatus;

[Authorize]
[EnableRateLimiting("driver_api")]
[Route("api/v1/drivers")]
public class GetDriverStatusController(IMediator mediator) : ControllerBase
{
  [EnableRateLimiting("driver_status")]
  [HttpGet("{driverId:guid}")]
  public async Task<ActionResult<DriverStatusResponse>> Get(Guid driverId, CancellationToken cancellationToken)
  {
    return Ok(await mediator.Send(new GetDriverStatus { Id = driverId }, cancellationToken));
  }
}
