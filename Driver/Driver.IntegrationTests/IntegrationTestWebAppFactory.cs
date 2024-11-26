using System.Data;
using Driver.Handlers.Models;
using Identity.Handlers.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Testcontainers.MsSql;
using Xunit;

namespace Driver.IntegrationTests
{
  public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
  {
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
                  .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                  .WithEnvironment("ACCEPT_EULA", "Y")
                  .WithEnvironment("MSSQL_SA_PASSWORD", "Strong!Passw0rd")
                  .WithEnvironment("SQLCMDPASSWORD", "Strong!Passw0rd")
                  .WithPassword("Strong!Passw0rd")
                  .WithPortBinding(1533)
                  .WithName("sqlserver_driver_integration_tests")
                  .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
      builder.ConfigureTestServices(services =>
      {
        var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<DriverContext>));
        if (descriptor != null)
        {
          services.Remove(descriptor);
        }

        services.AddDbContext<DriverContext>(options =>
              {
                options.UseSqlServer(_dbContainer.GetConnectionString(), a => a.UseNetTopologySuite());
              });

        services.AddDbContext<IdentityContext>(options =>
        {
          options.UseSqlServer(_dbContainer.GetConnectionString());
        });

        var sp = services.BuildServiceProvider();
        using (var scope = sp.CreateScope())
        {
          var scopedServices = scope.ServiceProvider;
          var db = scopedServices.GetRequiredService<DriverContext>();

          var connection = new SqlConnection(_dbContainer.GetConnectionString());
          if (connection.State == ConnectionState.Closed)
            connection.Open();

          var command = new SqlCommand("IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'driver') CREATE DATABASE [driver];", connection);

          command.ExecuteNonQuery();

          connection.Close();

          db.Database.Migrate();
        }

        using (var scope = sp.CreateScope())
        {
          var scopedServices = scope.ServiceProvider;
          var db = scopedServices.GetRequiredService<IdentityContext>();

          var connection = new SqlConnection(_dbContainer.GetConnectionString());
          if (connection.State == ConnectionState.Closed)
            connection.Open();

          var command = new SqlCommand("IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'identity') CREATE DATABASE [identity];", connection);

          command.ExecuteNonQuery();

          connection.Close();

          db.Database.Migrate();
        }

        services.AddOpenIddict()
                        .AddCore(options =>
                        {
                          options.UseEntityFrameworkCore()
                                .UseDbContext<IdentityContext>();
                        })
                        .AddServer(options =>
                        {
                          options.SetTokenEndpointUris("connect/token")
                                 .SetLogoutEndpointUris("connect/logout");

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
      });
    }

    public Task InitializeAsync()
    {
      return _dbContainer.StartAsync();
    }

    public new Task DisposeAsync()
    {
      return _dbContainer.StopAsync();
    }
  }
}
