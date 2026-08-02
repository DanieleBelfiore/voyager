using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Identity.Application.CQRS.Commands;
using Identity.Application.CQRS.Queries;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Identity.Api.Controllers;

/// <summary>
/// OpenID Connect endpoints. Register/Exchange delegate the actual business logic to
/// Application (RegisterUser / AuthenticateUser) — this controller only translates between
/// HTTP/OAuth2 protocol concerns and Application requests, unlike the Plugin.Microservices.CQRS
/// variant where validation and persistence happen inline in the controller.
/// </summary>
public class AuthController(IMediator mediator) : Controller
{
  [EnableRateLimiting("identity_register")]
  [HttpPost("~/connect/register")]
  [Produces("application/json")]
  public async Task<IActionResult> Register([FromBody] RegisterUser command)
  {
    try
    {
      await mediator.Send(command);
    }
    catch (Exception ex)
    {
      return BadRequest(new { ErrorDescription = ex.Message });
    }

    return Ok();
  }

  [EnableRateLimiting("identity_token")]
  [HttpPost("~/connect/token")]
  [Consumes("application/x-www-form-urlencoded")]
  [Produces("application/json")]
  public async Task<IActionResult> Exchange()
  {
    // A direct 500 here, not a thrown exception: the global exception handler now maps
    // InvalidOperationException to 409 for domain conflicts, and this guard is an internal
    // invariant (a misconfigured pipeline), not a client-facing conflict.
    if (HttpContext.GetOpenIddictServerRequest() is not { } request)
      return StatusCode(StatusCodes.Status500InternalServerError);

    if (!request.IsPasswordGrantType())
      return BadRequest(new OpenIddictResponse { Error = OpenIddictConstants.Errors.UnsupportedGrantType, ErrorDescription = "Only the password grant flow is supported." });

    var result = await mediator.Send(new AuthenticateUser { Username = request.Username, Password = request.Password });
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
