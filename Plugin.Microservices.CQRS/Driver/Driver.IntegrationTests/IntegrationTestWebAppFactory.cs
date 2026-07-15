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
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
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

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    var baseConnStr = _dbContainer.GetConnectionString();
    var driverConnStr = new SqlConnectionStringBuilder(baseConnStr) { InitialCatalog = "driver" }.ConnectionString;
    var identityConnStr = new SqlConnectionStringBuilder(baseConnStr) { InitialCatalog = "identity" }.ConnectionString;

    builder.ConfigureAppConfiguration(cfg =>
    {
      cfg.AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ConnectionStrings:DriverContext"] = driverConnStr,
        ["ConnectionStrings:IdentityContext"] = identityConnStr,
        ["RabbitMQ:HostName"] = _rabbitContainer.Hostname,
        ["RabbitMQ:Port"] = _rabbitContainer.GetMappedPublicPort(5672).ToString(),
        ["RabbitMQ:UserName"] = "testuser",
        ["RabbitMQ:Password"] = "testpass",
        ["Redis:ConnectionString"] = _redisContainer.GetConnectionString()
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
  }

  public new async Task DisposeAsync()
  {
    await Task.WhenAll(_dbContainer.StopAsync(), _rabbitContainer.StopAsync(), _redisContainer.StopAsync());
  }
}
