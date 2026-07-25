using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Features.RegisterUser;

public class RegisterUserController(IMediator mediator) : Controller
{
  [HttpPost("~/connect/register")]
  [Produces("application/json")]
  public async Task<IActionResult> Register([FromBody] RegisterUser command, CancellationToken cancellationToken)
  {
    try
    {
      await mediator.Send(command, cancellationToken);
    }
    catch (Exception ex)
    {
      return BadRequest(new { ErrorDescription = ex.Message });
    }

    return Ok();
  }
}
