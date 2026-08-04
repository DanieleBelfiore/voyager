using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Driver.Module.Features.AddDriver;

[Authorize]
[EnableRateLimiting("driver_api")]
[Route("api/v1/drivers")]
public class AddDriverController(IHikyaku mediator) : ControllerBase
{
  // Gated on the is_driver claim, not just authentication: a Driver row is what puts someone into
  // SearchBestDriver's candidate pool, so an unrestricted endpoint let any rider self-register,
  // publish a location, and be matched to real ride requests they can never accept.
  [Authorize(Policy = "RequireDriver")]
  [EnableRateLimiting("driver_registration")]
  [HttpPost]
  public async Task<ActionResult> Add(CancellationToken cancellationToken)
  {
    await mediator.Send(new Voyager.Contracts.Driver.AddDriver { DriverId = this.GetUserId() }, cancellationToken);

    return Ok();
  }
}
