using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Driver.Api.Features.UpdateAvailability;

[Authorize]
[EnableRateLimiting("driver_api")]
[Route("api/v1/drivers")]
public class UpdateAvailabilityController(IMediator mediator) : ControllerBase
{
  [EnableRateLimiting("driver_status_update")]
  [HttpPut("availability")]
  public async Task<ActionResult> Update([FromBody] UpdateAvailabilityRequest request, CancellationToken cancellationToken)
  {
    await mediator.Send(new UpdateAvailability { Id = this.GetUserId(), Status = request.Status }, cancellationToken);

    return Ok();
  }
}
