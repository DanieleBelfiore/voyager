using System;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voyager.Errors;

namespace Identity.Api.Features.RegisterUser;

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
      // Only these two carry a message safe to return: both are raised deliberately by
      // RegisterUserHandler and their text is a fixed code about the caller's own input. Format
      // errors never reach here — RegisterUserValidator raises ValidationException through the
      // pipeline behavior, which the global handler renders with per-field detail.
      return BadRequest(new { ErrorDescription = ex.Message });
    }
    catch (DbUpdateException ex)
    {
      // Concurrent registrations can race past the handler's own check and hit the unique index.
      logger.LogWarning(ex, "Registration failed on a database constraint violation");

      return BadRequest(new { ErrorDescription = "already_exist" });
    }
    catch (Exception ex)
    {
      // Everything else is an unexpected fault. Echoing ex.Message here leaked internal failure
      // text — SQL Server and EF Core messages included — to an unauthenticated caller.
      logger.LogError(ex, "User registration failed unexpectedly");

      return BadRequest(new { ErrorDescription = "registration_failed" });
    }

    return Ok();
  }
}
