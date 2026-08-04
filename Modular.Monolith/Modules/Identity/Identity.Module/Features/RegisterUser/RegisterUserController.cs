using System;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voyager.Errors;

namespace Identity.Module.Features.RegisterUser;

public class RegisterUserController(IHikyaku mediator, ILogger<RegisterUserController> logger) : Controller
{
  [EnableRateLimiting("identity_register")]
  [HttpPost("~/connect/register")]
  [Produces("application/json")]
  public async Task<IActionResult> Register([FromBody] RegisterUser command, CancellationToken cancellationToken)
  {
    try
    {
      await mediator.Send(command, cancellationToken);
    }
    catch (Exception ex) when (ex is InvalidInputException or ConflictException)
    {
      // Expected business-rule violation, matched by type rather than by message text: only
      // these two are raised deliberately by the handler, so only their message is a fixed code
      // safe to return. String-matching on the message would echo any exception that happened
      // to carry the same text.
      return BadRequest(new { ErrorDescription = ex.Message });
    }
    catch (DbUpdateException ex)
    {
      // Concurrent registrations can race past the app-level check and hit the unique index instead.
      logger.LogWarning(ex, "Registration failed on a database constraint violation");
      return BadRequest(new { ErrorDescription = "already_exist" });
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "User registration failed unexpectedly");
      return BadRequest(new { ErrorDescription = "registration_failed" });
    }

    return Ok();
  }
}
