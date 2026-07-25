using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Driver.Api.Features.AddDriver;

[Authorize]
[EnableRateLimiting("driver_api")]
[Route("api/v1/drivers")]
public class AddDriverController(IMediator mediator) : ControllerBase
{
  [EnableRateLimiting("driver_registration")]
  [HttpPost]
  public async Task<ActionResult> Add(CancellationToken cancellationToken)
  {
    await mediator.Send(new Voyager.Contracts.Driver.AddDriver { DriverId = this.GetUserId() }, cancellationToken);

    return Ok();
  }
}
