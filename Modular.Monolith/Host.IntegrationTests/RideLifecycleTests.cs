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
