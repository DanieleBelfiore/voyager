using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Identity.Api.Features.AuthenticateUser;

public class AuthenticateUserController(IMediator mediator) : Controller
{
  [HttpPost("~/connect/token")]
  [Consumes("application/x-www-form-urlencoded")]
  [Produces("application/json")]
  public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
  {
    var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

    if (!request.IsPasswordGrantType())
      return BadRequest(new OpenIddictResponse { Error = OpenIddictConstants.Errors.UnsupportedGrantType, ErrorDescription = "Only the password grant flow is supported." });

    var result = await mediator.Send(new AuthenticateUser { Username = request.Username, Password = request.Password }, cancellationToken);
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
