using System.Security.Claims;
using Identity.Handlers.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Driver.IntegrationTests;

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

    // Mirrors UsersController.CreatePrincipalAsync, the endpoint this stands in for. Both lines
    // matter: RequireDriver reads is_driver off the token, and a claim with no destination is
    // attached to the principal but silently dropped from the issued token — so without the
    // second line every claim added here would be invisible to the services validating it.
    principal.Identities.First().AddClaim(new Claim(Constants.IS_DRIVER, user.IsDriver.ToString()));
    principal.SetScopes(req.GetScopes());
    principal.SetDestinations(_ => [OpenIddictConstants.Destinations.AccessToken]);

    return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
  }
}
