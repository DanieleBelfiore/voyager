using Identity.Handlers.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Ride.API.Controllers;
using Ride.Handlers.Models;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Voyager.TestInfra;
using Xunit;

namespace Ride.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
  private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("MSSQL_SA_PASSWORD", "Strong!Passw0rd")
    .WithEnvironment("SQLCMDPASSWORD", "Strong!Passw0rd")
    .WithPassword("Strong!Passw0rd")
    .Build();

  private readonly RabbitMqContainer _rabbitContainer = new RabbitMqBuilder("rabbitmq:4.3-management")
    .WithUsername("testuser")
    .WithPassword("testpass")
    .Build();

  // The Driver module composes into this host too (see the project reference), and its handlers
  // take ICacheService — which Ride's own Program.cs backs with Redis.
  private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7").Build();

  private readonly DatabaseResetter _resetter = new();
  private string _rideConnectionString;

  public string RideConnectionString => _rideConnectionString ??=
    new SqlConnectionStringBuilder(_dbContainer.GetConnectionString()) { InitialCatalog = "ride" }.ConnectionString;

  public string DriverConnectionString =>
    new SqlConnectionStringBuilder(_dbContainer.GetConnectionString()) { InitialCatalog = "driver" }.ConnectionString;

  public string RedisConnectionString => _redisContainer.GetConnectionString();

  /// <summary>
  /// Truncates every table rather than dropping the database. The previous reset ran
  /// <c>EnsureDeleted</c> + <c>EnsureCreated</c>, which rebuilds the schema from the EF model and
  /// so discards everything a migration does outside it — indexes and constraints created by raw
  /// SQL among them. Tests then ran against a schema no deployment ever produces.
  /// </summary>
  public Task ResetDatabaseAsync() => _resetter.ResetAsync(RideConnectionString);

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    // Production, not the Development default: in Development the developer exception page answers
    // 500 for everything and hides the ProblemDetails mapping the tests assert on.
    builder.UseEnvironment(Environments.Production);

    var baseConnStr = _dbContainer.GetConnectionString();
    var identityConnStr = new SqlConnectionStringBuilder(baseConnStr) { InitialCatalog = "identity" }.ConnectionString;

    builder.ConfigureAppConfiguration(cfg =>
    {
      cfg.AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ConnectionStrings:RideContext"] = RideConnectionString,
        ["ConnectionStrings:DriverContext"] = DriverConnectionString,
        ["ConnectionStrings:IdentityContext"] = identityConnStr,
        ["Redis:ConnectionString"] = RedisConnectionString,
        ["RabbitMQ:HostName"] = _rabbitContainer.Hostname,
        ["RabbitMQ:Port"] = _rabbitContainer.GetMappedPublicPort(5672).ToString(),
        ["RabbitMQ:UserName"] = "testuser",
        ["RabbitMQ:Password"] = "testpass"
      });
    });

    builder.ConfigureTestServices(services =>
    {
      services.AddIdentity<VoyagerUser, VoyagerRole>().AddEntityFrameworkStores<IdentityContext>();

      // AddIdentity overrides the default auth scheme to cookie; re-set to OpenIddict so [Authorize] uses Bearer
      services.AddAuthentication(options =>
      {
        options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
      });

      // Register test assembly (TestTokenController) and Ride.API assembly (RideController)
      services.AddControllers()
        .AddApplicationPart(typeof(IntegrationTestWebAppFactory).Assembly)
        .AddApplicationPart(typeof(RidesController).Assembly);

      // Remove all OpenIddict services registered by the host (remote issuer validation conflicts with local test server)
      var openIddictDescriptors = services.Where(d => d.ServiceType.Namespace?.StartsWith("OpenIddict") == true ||
                                                      d.ImplementationType?.Namespace?.StartsWith("OpenIddict") == true)
        .ToList();
      foreach (var d in openIddictDescriptors)
      {
        services.Remove(d);
      }

      services.AddOpenIddict()
        .AddCore(options =>
        {
          options.UseEntityFrameworkCore().UseDbContext<IdentityContext>();
        })
        .AddServer(options =>
        {
          options.SetTokenEndpointUris("connect/token")
            .SetEndSessionEndpointUris("connect/logout");

          options.RegisterScopes(OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles);

          options.AllowPasswordFlow();

          options.UseAspNetCore()
            .EnableTokenEndpointPassthrough()
            .DisableTransportSecurityRequirement();

          options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

          options.DisableAccessTokenEncryption();
        })
        .AddValidation(options =>
        {
          options.UseLocalServer();
          options.UseAspNetCore();
        });

      var sp = services.BuildServiceProvider();
      using (var rideScope = sp.CreateScope()) {
        rideScope.ServiceProvider.GetRequiredService<RideContext>().Database.Migrate();
      }

      // Identity.Handlers.SQLMigrationContext holds all identity migrations (including voyager_app seed)
      using (var identityScope = sp.CreateScope()) {
        identityScope.ServiceProvider.GetRequiredService<Identity.Handlers.Models.SQLMigrationContext>().Database.Migrate();
      }
    });
  }

  public async Task InitializeAsync()
  {
    await Task.WhenAll(_dbContainer.StartAsync(), _rabbitContainer.StartAsync(), _redisContainer.StartAsync());

    // Environment variables, not just ConfigureAppConfiguration: a source added there is merged
    // when the host is built, too late for anything binding configuration eagerly at registration.
    // AddRedisCache does exactly that, so the host would otherwise keep the compose hostname and
    // every cache operation would quietly go nowhere.
    Environment.SetEnvironmentVariable("Redis__ConnectionString", RedisConnectionString);
  }

  public new async Task DisposeAsync()
  {
    await Task.WhenAll(_dbContainer.StopAsync(), _rabbitContainer.StopAsync(), _redisContainer.StopAsync());
  }
}
