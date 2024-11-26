using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Driver.Core.CQRS.Commands;
using Identity.Core.Dtos;
using Identity.Handlers.Models;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Identity.API.Controllers
{
  /// <summary>
  /// Authentication and authorization controller implementing OpenID Connect.
  /// Handles:
  /// - User registration with role-based permissions
  /// - Token-based authentication
  /// - Password grant flow
  /// - Session management
  /// - Integration with driver registration workflow
  /// </summary>
  public partial class UsersController(IdentityContext db, SignInManager<VoyagerUser> signInManager, UserManager<VoyagerUser> userManager, IMediator mediator) : Controller
  {
    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="model">The registration model containing the user details.</param>
    /// <returns>A JSON response indicating the outcome of the registration.</returns>
    [HttpPost("~/connect/register")]
    [Produces("application/json")]
    public async Task<IActionResult> Register([FromBody] Register model)
    {
      try
      {
        if (model.FirstName == null)
          throw new Exception("first_name_required");

        if (model.LastName == null)
          throw new Exception("last_name_required");

        if (model.Email == null || !CheckEmail().IsMatch(model.Email))
          throw new Exception("email_required");

        if (model.Password == null || model.Password.Length < 8)
          throw new Exception("password_string_length");

        if (model.Password != model.ConfirmPassword)
          throw new Exception("confirm_password_not_matching");

        var exuser = await db.Users.Where(f => f.Email == model.Email).FirstOrDefaultAsync();
        if (exuser != null)
          throw new Exception("already_exist");

        var user = new VoyagerUser
        {
          UserName = model.Email.Trim(),
          Email = model.Email.Trim(),
          FirstName = model.FirstName.Trim(),
          LastName = model.LastName.Trim(),
          PhoneNumber = model.PhoneNumber?.Trim(),
          IsDriver = model.IsDriver
        };

        var result = await userManager.CreateAsync(user, model.Password);

        await db.SaveChangesAsync();

        if (!result.Succeeded)
          throw new Exception("something_goes_wrong");

        if (user.IsDriver)
          await mediator.Send(new AddDriver { DriverId = user.Id });
      }
      catch (Exception ex)
      {
        return BadRequest(new { ErrorDescription = ex.Message });
      }

      return Ok();
    }

    /// <summary>
    /// Exchanges user credentials for an access token.
    /// </summary>
    /// <returns>A JSON response containing the access token.</returns>
    [HttpPost("~/connect/token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
      try
      {
        var req = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
        if (!req.IsPasswordGrantType())
          return BadRequest(new { ErrorDescription = new Exception("only_password_grant_flow_is_supported") });

        var user = await userManager.FindByNameAsync(req.Username ?? throw new InvalidOperationException());
        if (user == null)
          return BadRequest(new OpenIddictResponse { Error = OpenIddictConstants.Errors.InvalidGrant, ErrorDescription = "The username/password couple is invalid." });

        if (!await signInManager.CanSignInAsync(user))
          return BadRequest(new OpenIddictResponse { Error = OpenIddictConstants.Errors.InvalidGrant, ErrorDescription = "The specified user is not allowed to sign in." });

        if (!await userManager.CheckPasswordAsync(user, req.Password ?? throw new InvalidOperationException()))
          return BadRequest(new OpenIddictResponse { Error = OpenIddictConstants.Errors.InvalidGrant, ErrorDescription = "The username/password couple is invalid." });

        var principal = await CreatePrincipalAsync(req, user);

        user.LastLogin = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
      }
      catch (Exception ex)
      {
        return BadRequest(new { ErrorDescription = ex.Message });
      }
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    /// <returns>An IActionResult indicating the outcome of the logout process.</returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
      await signInManager.SignOutAsync();

      return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Creates the principal for the specified user.
    /// </summary>
    /// <param name="request">The OpenID Connect request.</param>
    /// <param name="user">The user for whom to create the principal.</param>
    /// <returns>A ClaimsPrincipal for the user.</returns>
    private async Task<ClaimsPrincipal> CreatePrincipalAsync(OpenIddictRequest request, VoyagerUser user)
    {
      var principal = await signInManager.CreateUserPrincipalAsync(user);

      var identity = principal.Identities.FirstOrDefault();

      identity?.AddClaim(ClaimTypes.NameIdentifier, user.Id.ToString());
      identity?.AddClaim(Constants.IS_DRIVER, user.IsDriver);

      var scopes = new List<string>(request.GetScopes());

      principal.SetScopes(scopes);

      return principal;
    }

    /// <summary>
    /// Checks if the email is valid.
    /// </summary>
    /// <returns>A Regex object for email validation.</returns>
    [GeneratedRegex(@"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase, "it-IT")]
    private static partial Regex CheckEmail();
  }
}
