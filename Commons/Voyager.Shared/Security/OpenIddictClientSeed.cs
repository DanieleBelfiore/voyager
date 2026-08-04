using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace Voyager.Shared.Security;

/// <summary>
/// Registers the public OpenIddict client that first-party callers authenticate as.
/// </summary>
/// <remarks>
/// OpenIddict authenticates the client before the user: a token request whose <c>client_id</c>
/// is absent from the application store is rejected outright with <c>invalid_client</c>. Nothing
/// in the request pipeline creates that row, so without this seed no token can ever be issued and
/// every <c>[Authorize]</c> endpoint in the variant is unreachable — including from its own Demo.
///
/// Seeded at startup rather than from a migration so it stays idempotent and independent of the
/// catalogue name, and so the permission set is written with OpenIddict's own constants instead
/// of a hand-maintained JSON array that drifts silently across major versions (<c>ept:logout</c>
/// became <c>ept:end_session</c>, for one).
///
/// Infra-agnostic on purpose: no domain or CQRS knowledge, just the client registration every
/// variant's Identity service needs. Kept here so the four variants that share
/// <c>Commons/</c> do not each carry a copy.
/// </remarks>
public static class OpenIddictClientSeed
{
  /// <summary>
  /// The client id Swagger's token form and each variant's <c>Demo/Program.cs</c> send.
  /// </summary>
  public const string PublicClientId = "voyager_app";

  public static async Task SeedVoyagerApplicationAsync(this IServiceProvider services)
  {
    var applications = services.GetRequiredService<IOpenIddictApplicationManager>();

    if (await applications.FindByClientIdAsync(PublicClientId) is not null)
      return;

    await applications.CreateAsync(new OpenIddictApplicationDescriptor
    {
      ClientId = PublicClientId,
      // Public, not confidential: the callers are a browser SPA and the demo console, neither of
      // which can hold a client secret. The password grant is what carries the user's own
      // credentials, and that is what is actually being authenticated here.
      ClientType = OpenIddictConstants.ClientTypes.Public,
      DisplayName = "Voyager App",
      Permissions =
      {
        OpenIddictConstants.Permissions.Endpoints.Token,
        OpenIddictConstants.Permissions.Endpoints.EndSession,
        OpenIddictConstants.Permissions.GrantTypes.Password,
        OpenIddictConstants.Permissions.ResponseTypes.Token,
        OpenIddictConstants.Permissions.ResponseTypes.IdToken,
        OpenIddictConstants.Permissions.ResponseTypes.IdTokenToken,
        OpenIddictConstants.Permissions.Scopes.Email,
        OpenIddictConstants.Permissions.Scopes.Profile,
        OpenIddictConstants.Permissions.Scopes.Roles
      }
    });
  }
}
