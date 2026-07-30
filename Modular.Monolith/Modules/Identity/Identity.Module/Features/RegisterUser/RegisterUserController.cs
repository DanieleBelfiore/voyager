using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Module.Features.RegisterUser;

public class RegisterUserController(IMediator mediator, ILogger<RegisterUserController> logger) : Controller
{
  [HttpPost("~/connect/register")]
  [Produces("application/json")]
  public async Task<IActionResult> Register([FromBody] RegisterUser command, CancellationToken cancellationToken)
  {
    try
    {
      await mediator.Send(command, cancellationToken);
    }
    catch (Exception ex) when (ex.Message == "already_exist")
    {
      // Expected business-rule violation: handler already checked for a duplicate email.
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
