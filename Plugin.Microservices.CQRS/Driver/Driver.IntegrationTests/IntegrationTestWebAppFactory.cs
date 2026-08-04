using Driver.API.Controllers;
using Driver.Handlers.Models;
using Identity.Handlers.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Respawn;
using Respawn.Graph;
using StackExchange.Redis;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace Driver.IntegrationTests;

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

  private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7").Build();

  private Respawner _respawner;
  private string _driverConnectionString;

  public string DriverConnectionString => _driverConnectionString ??=
    new SqlConnectionStringBuilder(_dbContainer.GetConnectionString()) { InitialCatalog = "driver" }.ConnectionString;

  public string RedisConnectionString => _redisContainer.GetConnectionString();

  /// <summary>
  /// The raw payload behind a cache key, or <c>null</c> if nothing was written. Read straight out
  /// of Redis rather than through <c>ICacheService</c>: the failure this guards against is a write
  /// that throws and gets swallowed, and the cache abstraction reports a miss either way.
  /// </summary>
  public async Task<string> ReadCacheEntry(string key)
  {
    using var redis = await ConnectionMultiplexer.ConnectAsync(RedisConnectionString);

    return await redis.GetDatabase().StringGetAsync(key);
  }

  /// <summary>
  /// Truncates every table rather than dropping the database. The previous reset ran
  /// <c>EnsureDeleted</c> + <c>EnsureCreated</c>, which rebuilds the schema from the EF model and
  /// so discards everything a migration does outside it — including
  /// <c>SIX_Drivers_LastLocation</c>, created by raw SQL in
  /// <c>20260731180239_AddDriverLastLocationSpatialIndex</c>. The geospatial query these tests
  /// exist to prove was running unindexed, and the migration itself was covered by nothing.
  /// </summary>
  public async Task ResetDatabaseAsync()
  {
    await using var connection = new SqlConnection(DriverConnectionString);
    await connection.OpenAsync();

    _respawner ??= await Respawner.CreateAsync(connection, new RespawnerOptions
    {
      DbAdapter = DbAdapter.SqlServer,
      TablesToIgnore = [new Table("__EFMigrationsHistory")]
    });

    await _respawner.ResetAsync(connection);
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    // Production, not the Development default. In Development the pipeline installs the developer
    // exception page, which answers 500 with a stack trace for everything — so the ProblemDetails
    // mapping that turns NotFoundException into 404 is never exercised and a test asserting on
    // status codes is asserting on behaviour no deployment has.
    builder.UseEnvironment(Environments.Production);

    var baseConnStr = _dbContainer.GetConnectionString();
    var identityConnStr = new SqlConnectionStringBuilder(baseConnStr) { InitialCatalog = "identity" }.ConnectionString;

    builder.ConfigureAppConfiguration(cfg =>
    {
      cfg.AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ConnectionStrings:DriverContext"] = DriverConnectionString,
        ["ConnectionStrings:IdentityContext"] = identityConnStr,
        ["RabbitMQ:HostName"] = _rabbitContainer.Hostname,
        ["RabbitMQ:Port"] = _rabbitContainer.GetMappedPublicPort(5672).ToString(),
        ["RabbitMQ:UserName"] = "testuser",
        ["RabbitMQ:Password"] = "testpass",
        ["Redis:ConnectionString"] = RedisConnectionString
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

      // Register test assembly (TestTokenController) and Driver.API assembly (DriverController)
      services.AddControllers()
        .AddApplicationPart(typeof(IntegrationTestWebAppFactory).Assembly)
        .AddApplicationPart(typeof(DriverController).Assembly);

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
      using (var driverScope = sp.CreateScope()) {
        driverScope.ServiceProvider.GetRequiredService<DriverContext>().Database.Migrate();
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

    // Environment variables, not just ConfigureAppConfiguration. A source added there is merged
    // when the host is built — after Program.cs has run its builder.Services.Add… calls. That is
    // invisible to anything reading configuration inside a factory lambda (the DbContext
    // registrations resolve their connection string per instance, so they see the override), and
    // fatal for anything reading it eagerly at registration: AddRedisCache binds
    // Redis:ConnectionString right there and hands the value to a singleton ConnectionMultiplexer,
    // so the host stayed pointed at the compose hostname and every cache write went nowhere.
    Environment.SetEnvironmentVariable("Redis__ConnectionString", RedisConnectionString);
  }

  public new async Task DisposeAsync()
  {
    await Task.WhenAll(_dbContainer.StopAsync(), _rabbitContainer.StopAsync(), _redisContainer.StopAsync());
  }
}
