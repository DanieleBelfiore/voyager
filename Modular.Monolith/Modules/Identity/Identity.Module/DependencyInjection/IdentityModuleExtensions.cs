using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Identity.Module.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Voyager.Shared.Security;

namespace Identity.Module.DependencyInjection;

/// <summary>
/// Owns the entire OpenIddict setup — both the authorization server (token issuance, tied to
/// the internal IdentityDbContext) and, via UseLocalServer, the validation side every other
/// module's [Authorize] attributes rely on. In the multi-service variants these were two
/// separate concerns split across services (Identity issues, everyone else validates over
/// HTTP); in one process there's no HTTP round-trip needed to validate a token issued by the
/// same server, so it collapses into a single AddOpenIddict() chain, registered once here.
/// </summary>
public static class IdentityModuleExtensions
{
  public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
  {
    services.AddDbContext<IdentityDbContext>((provider, options) =>
    {
      options.UseSqlServer(configuration.GetConnectionString("IdentityContext"));
      options.UseOpenIddict();
      options.AddInterceptors(provider.GetRequiredService<SlowQueryInterceptor>());
    });
    services.AddScoped<SlowQueryInterceptor>();

    services.AddHealthChecks().AddDbContextCheck<IdentityDbContext>("identity-database", tags: ["ready"]);

    services.AddSingleton(new PasswordHasher<object>());

    services.AddOpenIddict()
      .AddCore(options =>
      {
        options.UseEntityFrameworkCore().UseDbContext<IdentityDbContext>();
      })
      .AddServer(options =>
      {
        options.SetTokenEndpointUris("connect/token")
               .SetEndSessionEndpointUris("connect/logout");

        options.RegisterScopes(OpenIddictConstants.Scopes.Email, OpenIddictConstants.Scopes.Profile, OpenIddictConstants.Scopes.Roles);

        options.AllowPasswordFlow();

        options.UseAspNetCore()
          .EnableAuthorizationEndpointPassthrough()
          .EnableEndSessionEndpointPassthrough()
          .EnableTokenEndpointPassthrough()
          .DisableTransportSecurityRequirement();

        // Demo-scope relaxations, not for production: DisableTransportSecurityRequirement allows
        // the token endpoint over plain HTTP; the long token lifetimes and
        // DisableAccessTokenEncryption below keep the JWT plaintext and long-lived so it's easy
        // to inspect while developing. Tighten all of this before any non-demo deployment.
        options.SetAccessTokenLifetime(TimeSpan.FromHours(configuration.GetValue<double>("Identity:AccessTokenLifetimeHours")));
        options.SetIdentityTokenLifetime(TimeSpan.FromHours(configuration.GetValue<double>("Identity:IdentityTokenLifetimeHours")));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(configuration.GetValue<double>("Identity:RefreshTokenLifetimeDays")));

        options.DisableAccessTokenEncryption();

        // The docker-compose demo stack runs without ASPNETCORE_ENVIRONMENT, i.e. as Production,
        // so an IsDevelopment()-only guard takes the whole stack down at startup. The opt-in flag
        // is what keeps that stack working while still refusing to fall back to development
        // certificates by accident: nothing sets it outside docker-compose and launchSettings.
        if (environment.IsDevelopment() || configuration.GetValue<bool>("Identity:UseDevelopmentCertificates"))
        {
          options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
        }
        else
        {
          // No persisted-certificate loading path exists yet in this codebase (no config-bound
          // thumbprint/path pattern to follow). Fail fast rather than silently falling back to
          // ephemeral development certificates outside Development — they are regenerated per
          // machine, so two instances behind a load balancer would sign with different keys and
          // reject each other's tokens.
          throw new InvalidOperationException("Signing certificate must be configured for non-development environments");
        }

        options.SetIssuer(configuration["Identity:Issuer"]!);
      })
      .AddValidation(options =>
      {
        // No HTTP round-trip: tokens are validated against the server registered above,
        // in the same process — every module's [Authorize] attribute rides on this.
        options.UseLocalServer();
        options.UseAspNetCore();
      });

    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
    JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

    services.AddAuthentication(options =>
    {
      options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    });

    return services;
  }

  public static void MigrateIdentityDatabase(this IServiceProvider services)
  {
    using var scope = services.CreateScope();
    using var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    context.Database.Migrate();

    // Without this the variant cannot issue a token at all — see OpenIddictClientSeed.
    scope.ServiceProvider.SeedVoyagerApplicationAsync().GetAwaiter().GetResult();
  }
}
