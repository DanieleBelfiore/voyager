using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Identity.Core.Ports.Primary;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Voyager.Errors;

namespace Identity.Api.Controllers;

/// <summary>
/// Primary adapter over HTTP/OAuth2. Register/Exchange inject the primary ports
/// directly (IRegisterUserUseCase, IAuthenticateUserUseCase) — no IHikyaku.Send.
/// </summary>
public class AuthController(IRegisterUserUseCase registerUser, IAuthenticateUserUseCase authenticateUser, ILogger<AuthController> logger) : Controller
{
  [EnableRateLimiting("identity_register")]
  [HttpPost("~/connect/register")]
  [Produces("application/json")]
  public async Task<IActionResult> Register([FromBody] RegisterUser command, CancellationToken cancellationToken)
  {
    try
    {
      await registerUser.Handle(command, cancellationToken);
    }
    catch (Exception ex) when (ex is InvalidInputException or ConflictException)
    {
      // Only these two carry a message safe to return: both are raised deliberately by
      // RegisterUserUseCase and their text is a fixed code about the caller's own input.
      return BadRequest(new { ErrorDescription = ex.Message });
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

  [EnableRateLimiting("identity_token")]
  [HttpPost("~/connect/token")]
  [Consumes("application/x-www-form-urlencoded")]
  [Produces("application/json")]
  public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
  {
    // A direct 500 here, not a thrown exception: the global exception handler now maps
    // InvalidOperationException to 409 for domain conflicts, and this guard is an internal
    // invariant (a misconfigured pipeline), not a client-facing conflict.
    if (HttpContext.GetOpenIddictServerRequest() is not { } request)
      return StatusCode(StatusCodes.Status500InternalServerError);

    if (!request.IsPasswordGrantType())
      return BadRequest(new OpenIddictResponse { Error = OpenIddictConstants.Errors.UnsupportedGrantType, ErrorDescription = "Only the password grant flow is supported." });

    var result = await authenticateUser.Handle(new AuthenticateUser { Username = request.Username, Password = request.Password }, cancellationToken);
    if (!result.Succeeded)
      return BadRequest(new OpenIddictResponse { Error = OpenIddictConstants.Errors.InvalidGrant, ErrorDescription = result.ErrorDescription });

    var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, OpenIddictConstants.Claims.Name, OpenIddictConstants.Claims.Role);
    identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, result.UserId.ToString()));
    identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, result.Email));
    identity.AddClaim(new Claim(Constants.IsDriverClaimType, result.IsDriver.ToString()));

    var principal = new ClaimsPrincipal(identity);
    principal.SetScopes(request.GetScopes());

    foreach (var claim in principal.Claims)
      claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);

    return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
  }

  [ApiExplorerSettings(IgnoreApi = true)]
  [HttpPost("~/connect/logout")]
  public IActionResult Logout()
  {
    return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
  }
}
