extern alias identityApi;
extern alias driverApi;
extern alias rideApi;
extern alias hubApi;

using Microsoft.Extensions.DependencyInjection;
using Voyager.TestInfra;
using Xunit;

namespace Voyager.IntegrationTests;

/// <summary>
/// All four services booted side by side in one test process, against one set of containers.
/// </summary>
/// <remarks>
/// This is the shape the variant actually deploys — four independent hosts that only ever reach
/// each other through RabbitMQ or, for token validation, over HTTP. Booting one service alone
/// would leave both of those paths untested, which is most of what distinguishes this variant
/// from the modular monolith.
///
/// Identity comes up first: the other three resolve its discovery document during startup-time
/// validation setup, and they are handed its test-server handler to do it — see
/// <see cref="TestServerRouting"/>.
/// </remarks>
public class VoyagerStackFixture : IAsyncLifetime
{
  /// <summary>
  /// What every host is told the issuer is. It has to be the address the test server answers on,
  /// because that is where OpenIddict's validation will go looking for the discovery document.
  /// </summary>
  private const string Issuer = "http://localhost/";

  public VoyagerContainers Containers { get; } = new();
  public DatabaseResetter Databases { get; } = new();

  public VoyagerHost<identityApi::Program> Identity { get; private set; }
  public VoyagerHost<driverApi::Program> Driver { get; private set; }
  public VoyagerHost<rideApi::Program> Ride { get; private set; }
  public VoyagerHost<hubApi::Program> Hub { get; private set; }

  public async Task InitializeAsync()
  {
    await Containers.InitializeAsync();

    var configuration = Containers.Configuration();
    configuration["Identity:Issuer"] = Issuer;

    Containers.ExportToEnvironment();
    Environment.SetEnvironmentVariable("Identity__Issuer", Issuer);

    Identity = new VoyagerHost<identityApi::Program>(configuration).Start();

    void RouteToIdentity(IServiceCollection services) =>
      services.RouteOutboundHttpTo(() => Identity.Server.CreateHandler());

    Driver = new VoyagerHost<driverApi::Program>(configuration, RouteToIdentity).Start();
    Ride = new VoyagerHost<rideApi::Program>(configuration, RouteToIdentity).Start();
    Hub = new VoyagerHost<hubApi::Program>(configuration, RouteToIdentity).Start();
  }

  public async Task DisposeAsync()
  {
    foreach (var host in new IAsyncDisposable[] { Hub, Ride, Driver, Identity })
      if (host != null)
        await host.DisposeAsync();

    await Containers.DisposeAsync();
  }

  /// <summary>
  /// Clears the business catalogues between tests. The identity catalogue is left alone — it
  /// carries the OpenIddict client registration the token endpoint needs — and tests isolate
  /// themselves with the unique user <see cref="VoyagerAuth.RegisterAsync"/> creates per call.
  /// </summary>
  public Task ResetAsync()
  {
    return Task.WhenAll(
      Databases.ResetAsync(Containers.SqlConnectionString("driver")),
      Databases.ResetAsync(Containers.SqlConnectionString("ride")));
  }

  public async Task<VoyagerTestUser> RegisterAsync(bool isDriver)
  {
    return await VoyagerAuth.RegisterAsync(Identity.CreateClient(), isDriver);
  }

  public HttpClient ClientFor(VoyagerService service, VoyagerTestUser user = null)
  {
    var client = service switch
    {
      VoyagerService.Identity => Identity.CreateClient(),
      VoyagerService.Driver => Driver.CreateClient(),
      VoyagerService.Ride => Ride.CreateClient(),
      VoyagerService.Hub => Hub.CreateClient(),
      _ => throw new ArgumentOutOfRangeException(nameof(service))
    };

    if (user != null)
      client.Authenticate(user);

    return client;
  }
}

public enum VoyagerService
{
  Identity,
  Driver,
  Ride,
  Hub
}

[CollectionDefinition(Name)]
public class VoyagerCollection : ICollectionFixture<VoyagerStackFixture>
{
  public const string Name = "voyager-vertical-slice";
}

[Collection(VoyagerCollection.Name)]
public abstract class VoyagerIntegrationTest(VoyagerStackFixture fixture)
{
  protected VoyagerStackFixture Fixture { get; } = fixture;
}
