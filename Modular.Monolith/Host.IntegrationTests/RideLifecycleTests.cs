using System.Net.Http.Json;
using Driver.Module.Entities;
using NetTopologySuite.Geometries;
using Ride.Module.Entities;
using Ride.Module.Shared;
using Voyager.TestInfra;
using Xunit;

namespace Voyager.Host.IntegrationTests;

/// <summary>
/// The ride state machine across separate HTTP requests, each landing in SQL Server before the
/// next one reads it. Handler unit tests assert one transition against a context they seeded
/// themselves; what they cannot show is that the persisted state a real request leaves behind is
/// the state the next request's guard reads — the transitions here are refused out of order.
/// </summary>
public class RideLifecycleTests(VoyagerAppFixture fixture) : VoyagerIntegrationTest(fixture)
{
  private static readonly Point Pickup = new(12.4964, 41.9028) { SRID = 4326 };
  private static readonly Point Dropoff = new(12.5164, 41.9228) { SRID = 4326 };

  [Fact]
  public async Task ARide_MovesFromRequestedToCompleted_AndBothPartiesCanRateIt()
  {
    await Fixture.ResetAsync();

    var rider = Fixture.NewClient();
    var riderUser = await VoyagerAuth.RegisterAsync(rider, isDriver: false);
    rider.Authenticate(riderUser);

    var driver = Fixture.NewClient();
    var driverUser = await VoyagerAuth.RegisterAsync(driver, isDriver: true);
    driver.Authenticate(driverUser);

    await driver.PostAsync("api/v1/drivers", VoyagerJson.Content(new { })).ShouldSucceed();
    await driver.PutAsync("api/v1/drivers/location", VoyagerJson.Content(new { Location = Pickup })).ShouldSucceed();
    await driver.PutAsync("api/v1/drivers/availability", VoyagerJson.Content(new { Status = DriverStatus.Available })).ShouldSucceed();

    var requested = await rider.PostAsync("api/v1/rides", VoyagerJson.Content(new RequestRidePayload
    {
      DriverId = driverUser.Id,
      PickupLocation = Pickup,
      DropoffLocation = Dropoff
    })).ShouldSucceed();

    var ride = await requested.ReadAsync<RideDetailsResponse>();
    Assert.Equal(RideStatus.Requested, ride.Status);

    await driver.PutAsync($"api/v1/rides/{ride.Id}/accept", JsonContent.Create(new { })).ShouldSucceed();
    Assert.Equal(RideStatus.DriverAssigned, await ReadStatus(rider, ride.Id));

    await driver.PutAsync($"api/v1/rides/{ride.Id}/start", VoyagerJson.Content(new { Location = Pickup })).ShouldSucceed();
    Assert.Equal(RideStatus.InProgress, await ReadStatus(rider, ride.Id));

    await driver.PutAsync($"api/v1/rides/{ride.Id}/complete", VoyagerJson.Content(new { Location = Dropoff })).ShouldSucceed();

    var completed = await ReadRide(rider, ride.Id);
    Assert.Equal(RideStatus.Completed, completed.Status);
    Assert.NotNull(completed.EndAt);
    // The fare is computed on completion from the persisted pickup/dropoff, so a zero here would
    // mean the geometry did not survive the round trip even though the status did.
    Assert.True(completed.Price > 0, $"Completed ride carried no fare (price was {completed.Price}).");

    await rider.PutAsync($"api/v1/rides/{ride.Id}/rate/driver", VoyagerJson.Content(new { Rating = 5 })).ShouldSucceed();
    await driver.PutAsync($"api/v1/rides/{ride.Id}/rate", VoyagerJson.Content(new { Rating = 4 })).ShouldSucceed();
  }

  [Fact]
  public async Task ARide_CannotBeStarted_BeforeADriverHasAcceptedIt()
  {
    await Fixture.ResetAsync();

    var rider = Fixture.NewClient();
    var riderUser = await VoyagerAuth.RegisterAsync(rider, isDriver: false);
    rider.Authenticate(riderUser);

    var driver = Fixture.NewClient();
    var driverUser = await VoyagerAuth.RegisterAsync(driver, isDriver: true);
    driver.Authenticate(driverUser);

    await driver.PostAsync("api/v1/drivers", VoyagerJson.Content(new { })).ShouldSucceed();

    var requested = await rider.PostAsync("api/v1/rides", VoyagerJson.Content(new RequestRidePayload
    {
      DriverId = driverUser.Id,
      PickupLocation = Pickup,
      DropoffLocation = Dropoff
    })).ShouldSucceed();

    var ride = await requested.ReadAsync<RideDetailsResponse>();

    var started = await driver.PutAsync($"api/v1/rides/{ride.Id}/start", VoyagerJson.Content(new { Location = Pickup }));

    Assert.False(started.IsSuccessStatusCode,
      $"Starting a ride still in {RideStatus.Requested} was accepted with {(int)started.StatusCode}.");
  }

  private static async Task<RideStatus> ReadStatus(HttpClient client, Guid rideId)
  {
    return (await ReadRide(client, rideId)).Status;
  }

  private static async Task<RideDetailsResponse> ReadRide(HttpClient client, Guid rideId)
  {
    var response = await client.GetAsync($"api/v1/rides/{rideId}").ShouldSucceed();

    return await response.ReadAsync<RideDetailsResponse>();
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

    var driver = Fixture.NewClient();
    var driverUser = await VoyagerAuth.RegisterAsync(driver, isDriver: true);
    driver.Authenticate(driverUser);

    await driver.PostAsync("api/v1/drivers", VoyagerJson.Content(new { })).ShouldSucceed();
    await driver.PutAsync("api/v1/drivers/location", VoyagerJson.Content(new { Location = Pickup })).ShouldSucceed();
    await driver.PutAsync("api/v1/drivers/availability", VoyagerJson.Content(new { Status = DriverStatus.Available })).ShouldSucceed();

    var honest = await RunRideAsync(driver, driverUser.Id, Dropoff);
    var inflated = await RunRideAsync(driver, driverUser.Id, FarPastDropoff);

    Assert.Equal(honest.Price!.Value, inflated.Price!.Value, precision: 2);
    // The agreed destination also has to survive: overwriting it with the driver's coordinate
    // destroyed the only record of what the rider actually asked for.
    Assert.Equal(Dropoff.X, inflated.DropoffLocation.X, precision: 4);
    Assert.Equal(Dropoff.Y, inflated.DropoffLocation.Y, precision: 4);
  }

  /// <summary>A full ride for a fresh rider, completed at <paramref name="completionPoint"/>.</summary>
  private async Task<RideDetailsResponse> RunRideAsync(HttpClient driver, Guid driverId, Point completionPoint)
  {
    var rider = Fixture.NewClient();
    var riderUser = await VoyagerAuth.RegisterAsync(rider, isDriver: false);
    rider.Authenticate(riderUser);

    var ride = await (await rider.PostAsync("api/v1/rides", VoyagerJson.Content(new RequestRidePayload
    {
      DriverId = driverId,
      PickupLocation = Pickup,
      DropoffLocation = Dropoff
    })).ShouldSucceed()).ReadAsync<RideDetailsResponse>();

    await driver.PutAsync($"api/v1/rides/{ride.Id}/accept", JsonContent.Create(new { })).ShouldSucceed();
    await driver.PutAsync($"api/v1/rides/{ride.Id}/start", VoyagerJson.Content(new { Location = Pickup })).ShouldSucceed();
    await driver.PutAsync($"api/v1/rides/{ride.Id}/complete", VoyagerJson.Content(new { Location = completionPoint })).ShouldSucceed();

    return await ReadRide(rider, ride.Id);
  }

  // Roughly 20km past the agreed dropoff: far enough that pricing it instead would move the fare
  // by more than the assertion's tolerance.
  private static readonly Point FarPastDropoff = new(12.5164, 42.1228) { SRID = 4326 };

  /// <summary>
  /// Mirrors <c>RequestRideRequest</c>, which is internal to the Ride module. Declaring the shape
  /// here keeps the test on the module's HTTP contract rather than reaching past it.
  /// </summary>
  private class RequestRidePayload
  {
    public Guid DriverId { get; set; }
    public Point PickupLocation { get; set; }
    public Point DropoffLocation { get; set; }
  }
}
