using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Voyager.TestInfra;
using Xunit;

namespace Voyager.IntegrationTests;

/// <summary>
/// A ride event crossing two service boundaries and a transport: the Ride service publishes it,
/// Kaido carries it over RabbitMQ, the Hub service consumes it and relays it to a SignalR group,
/// and a client on the other end of a real connection receives it.
/// </summary>
/// <remarks>
/// The Hub unit tests assert the handler called the method on a mocked <c>IHubContext</c>.
/// Everything that decides whether a client actually hears it is outside that assertion: whether
/// the notification type published by Ride is the same type Hub's consumer is bound to, whether
/// the broker delivered it, whether the caller was admitted to <c>ride_{id}</c>, and whether the
/// group Hub publishes to is the group the client joined.
/// </remarks>
public class RideEventToSignalRTests(VoyagerStackFixture fixture) : VoyagerIntegrationTest(fixture)
{
  [Fact]
  public async Task CompletingARide_ReachesTheRidersConnectedClient_ViaTheBroker()
  {
    await Fixture.ResetAsync();

    var driverUser = await Fixture.RegisterAvailableDriverAsync(DriverWorkflow.Rome);
    var riderUser = await Fixture.RegisterAsync(isDriver: false);

    var rider = Fixture.ClientFor(VoyagerService.Ride, riderUser);
    var driver = Fixture.ClientFor(VoyagerService.Ride, driverUser);

    var ride = await (await rider.PostAsync("api/v1/rides", VoyagerJson.Content(new RequestRidePayload
    {
      DriverId = driverUser.Id,
      PickupLocation = DriverWorkflow.Rome,
      DropoffLocation = DriverWorkflow.AcrossTown
    })).ShouldSucceed()).ReadAsync<RideDetailsResult>();

    // JoinRideGroup authorises against an *active* ride, so the rider cannot be in ride_{id}
    // before this point — which is why the event under test is completion, not acceptance.
    await driver.PutAsync($"api/v1/rides/{ride.Id}/accept", JsonContent.Create(new { })).ShouldSucceed();

    await using var connection = BuildConnection(riderUser);

    var completed = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
    connection.On<Guid>("SendToRiderRideCompleted", rideId => completed.TrySetResult(rideId));

    // Bounded explicitly: a connection that never finishes its handshake would otherwise block
    // forever, and a hung suite says nothing about what broke.
    using var handshake = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await connection.StartAsync(handshake.Token);
    await connection.InvokeAsync("JoinRideGroup", ride.Id.ToString(), handshake.Token);

    await driver.PutAsync($"api/v1/rides/{ride.Id}/start", VoyagerJson.Content(new { Location = DriverWorkflow.Rome })).ShouldSucceed();
    await driver.PutAsync($"api/v1/rides/{ride.Id}/complete", VoyagerJson.Content(new { Location = DriverWorkflow.AcrossTown })).ShouldSucceed();

    // Generous, because this one waits on a broker round trip rather than an in-process call.
    var received = await Task.WhenAny(completed.Task, Task.Delay(TimeSpan.FromSeconds(30)));

    Assert.True(received == completed.Task, "The rider's client never received SendToRiderRideCompleted.");
    Assert.Equal(ride.Id, await completed.Task);
  }

  /// <summary>
  /// Long polling over the in-memory test server. WebSockets would need the handshake plumbed
  /// through <c>TestServer.CreateWebSocketClient</c>; the transport is not what is under test
  /// here, the publish/route/relay/group path is.
  /// </summary>
  private HubConnection BuildConnection(VoyagerTestUser user)
  {
    return new HubConnectionBuilder()
      .WithUrl(new Uri(Fixture.Hub.ClientOptions.BaseAddress, "voyagerhub"), options =>
      {
        options.HttpMessageHandlerFactory = _ => Fixture.Hub.Server.CreateHandler();
        options.Transports = HttpTransportType.LongPolling;
        options.AccessTokenProvider = () => Task.FromResult(user.AccessToken);
      })
      .Build();
  }
}
