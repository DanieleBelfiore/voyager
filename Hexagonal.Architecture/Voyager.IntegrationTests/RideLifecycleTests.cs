using System.Net.Http.Json;
using Voyager.TestInfra;
using Xunit;

namespace Voyager.IntegrationTests;

/// <summary>
/// The ride state machine across separate HTTP requests against the Ride service, each landing in
/// SQL Server before the next one reads it. Handler unit tests assert one transition against a
/// context they seeded themselves; what they cannot show is that the state a real request leaves
/// behind is the state the next request's guard reads — nor that the driver identity the Ride
/// service authorises against is the one Identity minted, a different service entirely.
/// </summary>
public class RideLifecycleTests(VoyagerStackFixture fixture) : VoyagerIntegrationTest(fixture)
{
  [Fact]
  public async Task ARide_MovesFromRequestedToCompleted_AndBothPartiesCanRateIt()
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

    Assert.Equal(RideState.Requested, ride.Status);

    await driver.PutAsync($"api/v1/rides/{ride.Id}/accept", JsonContent.Create(new { })).ShouldSucceed();
    Assert.Equal(RideState.DriverAssigned, (await ReadRide(rider, ride.Id)).Status);

    await driver.PutAsync($"api/v1/rides/{ride.Id}/start", VoyagerJson.Content(new { Location = DriverWorkflow.Rome })).ShouldSucceed();
    Assert.Equal(RideState.InProgress, (await ReadRide(rider, ride.Id)).Status);

    await driver.PutAsync($"api/v1/rides/{ride.Id}/complete", VoyagerJson.Content(new { Location = DriverWorkflow.AcrossTown })).ShouldSucceed();

    var completed = await ReadRide(rider, ride.Id);
    Assert.Equal(RideState.Completed, completed.Status);
    Assert.NotNull(completed.EndAt);
    // The fare is computed on completion from the persisted pickup/dropoff, so a zero here means
    // the geometry did not survive the round trip even though the status did.
    Assert.True(completed.Price > 0, $"Completed ride carried no fare (price was {completed.Price}).");

    // Both ratings travel to Identity over the broker; a failure here is a cross-service write,
    // not a local one.
    await rider.PutAsync($"api/v1/rides/{ride.Id}/rate/driver", VoyagerJson.Content(new { Rating = 5 })).ShouldSucceed();
    await driver.PutAsync($"api/v1/rides/{ride.Id}/rate", VoyagerJson.Content(new { Rating = 4 })).ShouldSucceed();
  }

  [Fact]
  public async Task ARide_CannotBeStarted_BeforeADriverHasAcceptedIt()
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

    var started = await driver.PutAsync($"api/v1/rides/{ride.Id}/start", VoyagerJson.Content(new { Location = DriverWorkflow.Rome }));

    Assert.False(started.IsSuccessStatusCode,
      $"Starting a ride still in {RideState.Requested} was accepted with {(int)started.StatusCode}.");
  }

  private static async Task<RideDetailsResult> ReadRide(HttpClient client, Guid rideId)
  {
    var response = await client.GetAsync($"api/v1/rides/{rideId}").ShouldSucceed();

    return await response.ReadAsync<RideDetailsResult>();
  }
}
