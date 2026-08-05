using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Driver.Api.Features.AddDriver;

[Authorize]
[EnableRateLimiting("driver_api")]
[Route("api/v1/drivers")]
public class AddDriverController(IHikyaku mediator) : ControllerBase
{
  // Gated on the is_driver claim, not just authentication: a Driver row is what puts someone into
  // SearchBestDriver's candidate pool, so an unrestricted endpoint let a rider account self-register,
  // publish a location, and be matched to real ride requests it can never accept. The claim marks
  // the account type chosen at registration, not a privilege granted by anyone — registering as a
  // driver is self-service, so this separates the two flows rather than keeping anyone out.
  [Authorize(Policy = "RequireDriver")]
  [EnableRateLimiting("driver_registration")]
  [HttpPost]
  public async Task<ActionResult> Add(CancellationToken cancellationToken)
  {
    await mediator.Send(new Voyager.Contracts.Driver.AddDriver { DriverId = this.GetUserId() }, cancellationToken);

    return Ok();
  }
}
