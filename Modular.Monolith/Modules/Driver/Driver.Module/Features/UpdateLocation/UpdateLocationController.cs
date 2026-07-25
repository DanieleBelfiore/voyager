using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Driver.Module.Features.UpdateLocation;

[Authorize]
[EnableRateLimiting("driver_api")]
[Route("api/v1/drivers")]
public class UpdateLocationController(IMediator mediator) : ControllerBase
{
  [EnableRateLimiting("driver_location_update")]
  [HttpPut("location")]
  public async Task<ActionResult> Update([FromBody] UpdateLocationRequest request, CancellationToken cancellationToken)
  {
    await mediator.Send(new Voyager.Contracts.Driver.UpdateLocation { Id = this.GetUserId(), Location = request.Location }, cancellationToken);

    return Ok();
  }
}
