using System.Net.Http.Json;
using NetTopologySuite.Geometries;
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

  /// <summary>
  /// The fare has to come off the route the rider agreed to, not off the coordinate the driver
  /// posts at completion — the driver is the party being paid by the kilometre. Run as a
  /// differential rather than a fixed number so it needs no knowledge of the fare configuration:
  /// two identical rides, one completed honestly and one completed at a point far past the agreed
  /// dropoff, must be charged the same. The lifecycle test above cannot catch this because it
  /// completes at exactly the requested dropoff, where both implementations agree.
  /// </summary>
  [Fact]
  public async Task TheFareIsUnchanged_WhenADriverCompletesFarPastTheAgreedDropoff()
  {
    await Fixture.ResetAsync();

    var driverUser = await Fixture.RegisterAvailableDriverAsync(DriverWorkflow.Rome);
    var driver = Fixture.ClientFor(VoyagerService.Ride, driverUser);

    var honest = await RunRideAsync(driver, driverUser.Id, DriverWorkflow.AcrossTown);
    var inflated = await RunRideAsync(driver, driverUser.Id, FarPastAcrossTown);

    Assert.Equal(honest.Price!.Value, inflated.Price!.Value, precision: 2);
    // The agreed destination also has to survive: overwriting it with the driver's coordinate
    // destroyed the only record of what the rider actually asked for.
    Assert.Equal(DriverWorkflow.AcrossTown.X, inflated.DropoffLocation.X, precision: 4);
    Assert.Equal(DriverWorkflow.AcrossTown.Y, inflated.DropoffLocation.Y, precision: 4);
  }

  /// <summary>A full ride for a fresh rider, completed at <paramref name="completionPoint"/>.</summary>
  private async Task<RideDetailsResult> RunRideAsync(HttpClient driver, Guid driverId, Point completionPoint)
  {
    var rider = Fixture.ClientFor(VoyagerService.Ride, await Fixture.RegisterAsync(isDriver: false));

    var ride = await (await rider.PostAsync("api/v1/rides", VoyagerJson.Content(new RequestRidePayload
    {
      DriverId = driverId,
      PickupLocation = DriverWorkflow.Rome,
      DropoffLocation = DriverWorkflow.AcrossTown
    })).ShouldSucceed()).ReadAsync<RideDetailsResult>();

    await driver.PutAsync($"api/v1/rides/{ride.Id}/accept", JsonContent.Create(new { })).ShouldSucceed();
    await driver.PutAsync($"api/v1/rides/{ride.Id}/start", VoyagerJson.Content(new { Location = DriverWorkflow.Rome })).ShouldSucceed();
    await driver.PutAsync($"api/v1/rides/{ride.Id}/complete", VoyagerJson.Content(new { Location = completionPoint })).ShouldSucceed();

    return await ReadRide(rider, ride.Id);
  }

  // Roughly 20km past the agreed dropoff: far enough that pricing it instead would move the fare
  // by more than the assertion's tolerance.
  private static readonly Point FarPastAcrossTown = new(12.5164, 42.1228) { SRID = 4326 };
}
