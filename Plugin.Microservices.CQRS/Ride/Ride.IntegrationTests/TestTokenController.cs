using Identity.Handlers.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Ride.IntegrationTests;

[ApiController]
public class TestTokenController(
  UserManager<VoyagerUser> userManager,
  SignInManager<VoyagerUser> signInManager) : ControllerBase
{
  [HttpPost("~/connect/token")]
  [Consumes("application/x-www-form-urlencoded")]
  public async Task<IActionResult> Exchange()
  {
    var req = HttpContext.GetOpenIddictServerRequest()
              ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

    if (!req.IsPasswordGrantType())
      return BadRequest(new OpenIddictResponse { Error = OpenIddictConstants.Errors.UnsupportedGrantType });

    var user = await userManager.FindByNameAsync(req.Username ?? string.Empty);
    if (user is null || !await userManager.CheckPasswordAsync(user, req.Password ?? string.Empty))
      return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

    var principal = await signInManager.CreateUserPrincipalAsync(user);

    // OpenIddict requires explicit sub claim
    principal.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
    principal.SetScopes(req.GetScopes());

    return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
  }
}
