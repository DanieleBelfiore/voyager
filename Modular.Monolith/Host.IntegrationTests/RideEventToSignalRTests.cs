using System.Net.Http.Json;
using Driver.Module.Entities;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using NetTopologySuite.Geometries;
using Ride.Module.Shared;
using Voyager.TestInfra;
using Xunit;

namespace Voyager.Host.IntegrationTests;

/// <summary>
/// A ride event travelling all the way to a connected client: the Ride module publishes a
/// notification, the Hub module's handler relays it to a SignalR group, and the client on the
/// other end of a real transport receives it.
/// </summary>
/// <remarks>
/// The Hub unit tests assert that the handler called <c>SendToRiderRideCompleted</c> on a mocked
/// <c>IHubContext</c>. Everything that decides whether a real client actually hears it sits
/// outside that assertion: whether SignalR can build a proxy for the typed client interface at
/// all, whether the caller was admitted to <c>ride_{id}</c>, and whether the group the handler
/// publishes to is the group the client joined. The first of those was broken — the typed client
/// interface is internal, and the runtime proxy could not implement it — which took out every
/// endpoint that raises an event, not just the notification.
/// </remarks>
public class RideEventToSignalRTests(VoyagerAppFixture fixture) : VoyagerIntegrationTest(fixture)
{
  private static readonly Point Pickup = new(12.4964, 41.9028) { SRID = 4326 };
  private static readonly Point Dropoff = new(12.5164, 41.9228) { SRID = 4326 };

  [Fact]
  public async Task CompletingARide_ReachesTheRidersConnectedClient()
  {
    await Fixture.ResetAsync();

    // Its own host, sharing the containers. A long-polling connection keeps a request open on the
    // in-memory test server for as long as it lives, and on the host every other test drives that
    // pins the server: the suite stops making progress. Disposing this host at the end of the
    // test is what reliably tears the poll down.
    await using var app = new VoyagerHost<Program>(Fixture.Containers.Configuration()).Start();

    var rider = app.CreateClient();
    var riderUser = await VoyagerAuth.RegisterAsync(rider, isDriver: false);
    rider.Authenticate(riderUser);

    var driver = app.CreateClient();
    var driverUser = await VoyagerAuth.RegisterAsync(driver, isDriver: true);
    driver.Authenticate(driverUser);

    await driver.PostAsync("api/v1/drivers", VoyagerJson.Content(new { })).ShouldSucceed();
    await driver.PutAsync("api/v1/drivers/location", VoyagerJson.Content(new { Location = Pickup })).ShouldSucceed();
    await driver.PutAsync("api/v1/drivers/availability", VoyagerJson.Content(new { Status = DriverStatus.Available })).ShouldSucceed();

    var ride = await (await rider.PostAsync("api/v1/rides", VoyagerJson.Content(new
    {
      DriverId = driverUser.Id,
      PickupLocation = Pickup,
      DropoffLocation = Dropoff
    })).ShouldSucceed()).ReadAsync<RideDetailsResponse>();

    // JoinRideGroup authorises against an *active* ride, so the rider cannot be in ride_{id}
    // before this point — which is exactly why the event under test is completion, not acceptance.
    await driver.PutAsync($"api/v1/rides/{ride.Id}/accept", JsonContent.Create(new { })).ShouldSucceed();

    await using var connection = BuildConnection(app, riderUser);

    var completed = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
    connection.On<Guid>("SendToRiderRideCompleted", rideId => completed.TrySetResult(rideId));

    // Bounded explicitly: a hub connection that never completes its handshake blocks forever
    // otherwise, and a hung suite says nothing about what broke.
    using var handshake = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await connection.StartAsync(handshake.Token);
    await connection.InvokeAsync("JoinRideGroup", ride.Id.ToString(), handshake.Token);

    await driver.PutAsync($"api/v1/rides/{ride.Id}/start", VoyagerJson.Content(new { Location = Pickup })).ShouldSucceed();
    await driver.PutAsync($"api/v1/rides/{ride.Id}/complete", VoyagerJson.Content(new { Location = Dropoff })).ShouldSucceed();

    var received = await Task.WhenAny(completed.Task, Task.Delay(TimeSpan.FromSeconds(15)));

    Assert.True(received == completed.Task, "The rider's client never received SendToRiderRideCompleted.");
    Assert.Equal(ride.Id, await completed.Task);
  }

  /// <summary>
  /// Long polling over the in-memory test server. WebSockets would need the handshake plumbed
  /// through <c>TestServer.CreateWebSocketClient</c>; the transport is not what is under test
  /// here, the publish/relay/group path is.
  /// </summary>
  private static HubConnection BuildConnection(VoyagerHost<Program> app, VoyagerTestUser user)
  {
    return new HubConnectionBuilder()
      .WithUrl(new Uri(app.ClientOptions.BaseAddress, "voyagerhub"), options =>
      {
        options.HttpMessageHandlerFactory = _ => app.Server.CreateHandler();
        options.Transports = HttpTransportType.LongPolling;
        options.AccessTokenProvider = () => Task.FromResult(user.AccessToken);
      })
      .Build();
  }
}
