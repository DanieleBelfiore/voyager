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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Identity.API.Controllers;

/// <summary>
/// Authentication and authorization controller implementing OpenID Connect.
/// Handles:
/// - User registration with role-based permissions
/// - Token-based authentication
/// - Password grant flow
/// - Session management
/// - Integration with driver registration workflow
/// </summary>
public partial class UsersController(IdentityContext db, SignInManager<VoyagerUser> signInManager, UserManager<VoyagerUser> userManager, IMediator mediator, ILogger<UsersController> logger) : Controller
{
  /// <summary>
  /// Registers a new user.
  /// </summary>
  /// <param name="model">The registration model containing the user details.</param>
  /// <returns>A JSON response indicating the outcome of the registration.</returns>
  [EnableRateLimiting("identity_register")]
  [HttpPost("~/connect/register")]
  [Produces("application/json")]
  public async Task<IActionResult> Register([FromBody] Register model)
  {
    if (model == null)
      return BadRequest(new { ErrorDescription = "invalid_request" });

    var validationError = Validate(model);
    if (validationError != null)
      return BadRequest(new { ErrorDescription = validationError });

    var email = model.Email.Trim();

    var exuser = await db.Users.Where(f => f.Email == email).FirstOrDefaultAsync();
    if (exuser != null)
      return BadRequest(new { ErrorDescription = "already_exist" });

    var user = new VoyagerUser
    {
      UserName = email,
      Email = email,
      FirstName = model.FirstName.Trim(),
      LastName = model.LastName.Trim(),
      PhoneNumber = model.PhoneNumber?.Trim(),
      IsDriver = model.IsDriver
    };

    // CreateAsync persists on its own. The SaveChangesAsync that used to sit here ran *before*
    // result.Succeeded was inspected, so a rejected registration still committed whatever else
    // the context happened to be tracking, and the failure was then reported as an opaque
    // "something_goes_wrong" with Identity's own reasons (password too short, duplicate
    // username) thrown away. Those codes describe the caller's own input, so they are safe and
    // useful to return.
    IdentityResult result;
    try
    {
      result = await userManager.CreateAsync(user, model.Password);
    }
    catch (DbUpdateException ex)
    {
      // Two concurrent registrations for the same email can both pass the check above and race
      // into the unique index. Reported as the same conflict the in-memory check already gives,
      // rather than as the 500 an uncaught store failure would produce.
      logger.LogWarning(ex, "Registration failed on a database constraint violation");

      return BadRequest(new { ErrorDescription = "already_exist" });
    }

    if (!result.Succeeded)
      return BadRequest(new { ErrorDescription = "registration_rejected", Errors = result.Errors.Select(f => f.Code).ToArray() });

    if (user.IsDriver && !await TryRegisterAsDriverAsync(user))
      return BadRequest(new { ErrorDescription = "driver_registration_failed" });

    return Ok();
  }

  /// <summary>
  /// Creates the Driver row that puts this user into SearchBestDriver's candidate pool, and
  /// undoes the user itself if that fails.
  ///
  /// Without the compensation the two writes could disagree permanently: the user existed but
  /// the driver did not, the caller was told registration failed, and retrying hit
  /// "already_exist" forever — leaving a driver who can never be matched to a ride. The two
  /// stores are in different services so there is no transaction to lean on; deleting the user
  /// is what makes the failure retryable.
  /// </summary>
  private async Task<bool> TryRegisterAsDriverAsync(VoyagerUser user)
  {
    try
    {
      await mediator.Send(new AddDriver { DriverId = user.Id });

      return true;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Driver registration failed for user {UserId}; rolling the user back", user.Id);

      var deleted = await userManager.DeleteAsync(user);
      if (!deleted.Succeeded)
        logger.LogError("Could not roll back user {UserId} after a failed driver registration", user.Id);

      return false;
    }
  }

  private static string Validate(Register model)
  {
    if (string.IsNullOrWhiteSpace(model.FirstName))
      return "first_name_required";

    if (string.IsNullOrWhiteSpace(model.LastName))
      return "last_name_required";

    if (model.Email == null || !CheckEmail().IsMatch(model.Email))
      return "email_required";

    if (model.Password == null || model.Password.Length < 8)
      return "password_string_length";

    if (model.Password != model.ConfirmPassword)
      return "confirm_password_not_matching";

    return null;
  }

  /// <summary>
  /// Exchanges user credentials for an access token.
  /// </summary>
  /// <returns>A JSON response containing the access token.</returns>
  [EnableRateLimiting("identity_token")]
  [HttpPost("~/connect/token")]
  [Consumes("application/x-www-form-urlencoded")]
  [Produces("application/json")]
  public async Task<IActionResult> Exchange()
  {
    // A 500, not a thrown exception echoed back: a missing OpenIddict request means the server
    // pipeline is misconfigured, which is an internal fault and not something the caller did.
    if (HttpContext.GetOpenIddictServerRequest() is not { } req)
      return StatusCode(StatusCodes.Status500InternalServerError);

    if (!req.IsPasswordGrantType())
      return BadRequest(new OpenIddictResponse { Error = OpenIddictConstants.Errors.UnsupportedGrantType, ErrorDescription = "Only the password grant flow is supported." });

    // One shared message for "no such user", "wrong password" and "not allowed to sign in": the
    // three are indistinguishable to the caller on purpose, so a failed grant reveals nothing
    // about whether the account exists.
    if (string.IsNullOrEmpty(req.Username) || string.IsNullOrEmpty(req.Password))
      return BadRequest(new OpenIddictResponse { Error = OpenIddictConstants.Errors.InvalidGrant, ErrorDescription = "The username/password couple is invalid." });

    var user = await userManager.FindByNameAsync(req.Username);
    if (user == null || !await signInManager.CanSignInAsync(user) || !await userManager.CheckPasswordAsync(user, req.Password))
      return BadRequest(new OpenIddictResponse { Error = OpenIddictConstants.Errors.InvalidGrant, ErrorDescription = "The username/password couple is invalid." });

    var principal = await CreatePrincipalAsync(req, user);

    user.LastLogin = DateTime.UtcNow;

    await db.SaveChangesAsync();

    // Anything unexpected past this point propagates to the global exception handler, which
    // returns a generic error. Echoing ex.Message here surfaced internal failure text — SQL and
    // EF messages included — straight to an unauthenticated caller.
    return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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
    // Not the AddClaim(string, bool) overload: it stringifies as JSON-style "true"/"false",
    // but RequireDriver's policy (Ride/Program.cs) checks for .NET's bool.ToString() — "True".
    identity?.AddClaim(new Claim(Constants.IS_DRIVER, user.IsDriver.ToString()));

    var scopes = new List<string>(request.GetScopes());

    principal.SetScopes(scopes);

    // OpenIddict.AddClaim(...) attaches a claim to the principal but gives it no destination,
    // so it's silently dropped from the issued access token unless told otherwise — every claim
    // added above (is_driver in particular, which [Authorize(Policy = "RequireDriver")] reads
    // from the token) would never actually reach the token without this.
    principal.SetDestinations(_ => [OpenIddictConstants.Destinations.AccessToken]);

    return principal;
  }

  /// <summary>
  /// Checks if the email is valid.
  /// </summary>
  /// <returns>A Regex object for email validation.</returns>
  [GeneratedRegex(@"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase, "it-IT")]
  private static partial Regex CheckEmail();
}
