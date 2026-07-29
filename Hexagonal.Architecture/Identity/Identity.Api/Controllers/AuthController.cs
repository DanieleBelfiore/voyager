using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Identity.Core.Ports.Primary;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Identity.Api.Controllers;

/// <summary>
/// Primary adapter over HTTP/OAuth2. Register/Exchange inject the primary ports
/// directly (IRegisterUserUseCase, IAuthenticateUserUseCase) — no IMediator.Send.
/// </summary>
public class AuthController(IRegisterUserUseCase registerUser, IAuthenticateUserUseCase authenticateUser) : Controller
{
  [HttpPost("~/connect/register")]
  [Produces("application/json")]
  public async Task<IActionResult> Register([FromBody] RegisterUser command, CancellationToken cancellationToken)
  {
    try
    {
      await registerUser.Handle(command, cancellationToken);
    }
    catch (Exception ex)
    {
      return BadRequest(new { ErrorDescription = ex.Message });
    }

    return Ok();
  }

  [HttpPost("~/connect/token")]
  [Consumes("application/x-www-form-urlencoded")]
  [Produces("application/json")]
  public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
  {
    var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

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
