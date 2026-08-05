using System.Net.Http.Json;
using System.Text;
using Driver.Core.Enums;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;
using Ride.Core.Dtos;
using Ride.Core.Enums;
using Xunit;

namespace Ride.IntegrationTests;

/// <summary>
/// The ride state machine across separate HTTP requests, each landing in SQL Server before the
/// next one reads it. Handler unit tests assert one transition against a context they seeded
/// themselves; what they cannot show is that the state a real request leaves behind is the state
/// the next request's guard reads — the transitions here are refused out of order — nor that the
/// caller the service authorises is the one the token was issued to.
/// </summary>
public class RideLifecycleTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
  private static readonly JsonSerializerSettings JsonSettings = new() { Converters = { new GeometryConverter() } };

  private static readonly Point Pickup = new(12.4964, 41.9028) { SRID = 4326 };
  private static readonly Point Dropoff = new(12.5164, 41.9228) { SRID = 4326 };

  [Fact]
  public async Task ARide_MovesFromRequestedToCompleted_AndBothPartiesCanRateIt()
  {
    var (rider, _) = await NewAuthenticatedClientAsync(isDriver: false);
    var (driver, driverId) = await NewAuthenticatedClientAsync(isDriver: true);

    await SeedAvailableDriver(driverId);

    var ride = await RequestRide(rider, driverId);
    Assert.Equal(RideStatus.Requested, ride.Status);

    await Put(driver, $"api/v1/rides/{ride.Id}/accept", new { });
    Assert.Equal(RideStatus.DriverAssigned, (await ReadRide(rider, ride.Id)).Status);

    await Put(driver, $"api/v1/rides/{ride.Id}/start", new { Location = Pickup });
    Assert.Equal(RideStatus.InProgress, (await ReadRide(rider, ride.Id)).Status);

    await Put(driver, $"api/v1/rides/{ride.Id}/complete", new { Location = Dropoff });

    var completed = await ReadRide(rider, ride.Id);
    Assert.Equal(RideStatus.Completed, completed.Status);
    Assert.NotNull(completed.EndAt);
    // The fare is computed on completion from the persisted pickup/dropoff, so a zero here would
    // mean the geometry did not survive the round trip even though the status did.
    Assert.True(completed.Price > 0, $"Completed ride carried no fare (price was {completed.Price}).");

    await Put(rider, $"api/v1/rides/{ride.Id}/rate/driver", new { Rating = 5 });
    await Put(driver, $"api/v1/rides/{ride.Id}/rate", new { Rating = 4 });
  }

  [Fact]
  public async Task ARide_CannotBeStarted_BeforeADriverHasAcceptedIt()
  {
    var (rider, _) = await NewAuthenticatedClientAsync(isDriver: false);
    var (driver, driverId) = await NewAuthenticatedClientAsync(isDriver: true);

    await SeedAvailableDriver(driverId);

    var ride = await RequestRide(rider, driverId);

    var started = await driver.PutAsync($"api/v1/rides/{ride.Id}/start", Body(new { Location = Pickup }));

    Assert.False(started.IsSuccessStatusCode,
      $"Starting a ride still in {RideStatus.Requested} was accepted with {(int)started.StatusCode}.");
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
    var (driver, driverId) = await NewAuthenticatedClientAsync(isDriver: true);

    await SeedAvailableDriver(driverId);

    var honest = await RunRideAsync(driver, driverId, Dropoff);
    var inflated = await RunRideAsync(driver, driverId, FarPastDropoff);

    Assert.Equal(honest.Price!.Value, inflated.Price!.Value, precision: 2);
    // The agreed destination also has to survive: overwriting it with the driver's coordinate
    // destroyed the only record of what the rider actually asked for.
    Assert.Equal(Dropoff.X, inflated.DropoffLocation.X, precision: 4);
    Assert.Equal(Dropoff.Y, inflated.DropoffLocation.Y, precision: 4);
  }

  /// <summary>A full ride for a fresh rider, completed at <paramref name="completionPoint"/>.</summary>
  private async Task<RideDetailsResponse> RunRideAsync(HttpClient driver, Guid driverId, Point completionPoint)
  {
    var (rider, _) = await NewAuthenticatedClientAsync(isDriver: false);

    var ride = await RequestRide(rider, driverId);

    await Put(driver, $"api/v1/rides/{ride.Id}/accept", new { });
    await Put(driver, $"api/v1/rides/{ride.Id}/start", new { Location = Pickup });
    await Put(driver, $"api/v1/rides/{ride.Id}/complete", new { Location = completionPoint });

    return await ReadRide(rider, ride.Id);
  }

  // Roughly 20km past the agreed dropoff: far enough that pricing it instead would move the fare
  // by more than the assertion's tolerance.
  private static readonly Point FarPastDropoff = new(12.5164, 42.1228) { SRID = 4326 };

  /// <summary>
  /// Seeded straight into the Driver catalogue rather than over HTTP: this host composes the
  /// Driver module but not its controllers, so there is no driver endpoint to call here.
  /// </summary>
  private async Task SeedAvailableDriver(Guid driverId)
  {
    await ResetDatabaseAsync();

    DriverContext.Drivers.Add(new Driver.Handlers.Models.Driver
    {
      Id = driverId,
      Status = DriverStatus.Available,
      LastLocation = Pickup,
      LastUpdateDate = DateTime.UtcNow
    });

    await DriverContext.SaveChangesAsync(CancellationToken.None);
  }

  private async Task<RideDetailsResponse> RequestRide(HttpClient rider, Guid driverId)
  {
    var response = await rider.PostAsync("api/v1/rides", Body(new
    {
      DriverId = driverId,
      PickupLocation = Pickup,
      DropoffLocation = Dropoff
    }));

    await EnsureSuccess(response, "POST api/v1/rides");

    return JsonConvert.DeserializeObject<RideDetailsResponse>(await response.Content.ReadAsStringAsync(), JsonSettings);
  }

  private static async Task<RideDetailsResponse> ReadRide(HttpClient client, Guid rideId)
  {
    var response = await client.GetAsync($"api/v1/rides/{rideId}");
    await EnsureSuccess(response, $"GET api/v1/rides/{rideId}");

    return JsonConvert.DeserializeObject<RideDetailsResponse>(await response.Content.ReadAsStringAsync(), JsonSettings);
  }

  private static async Task Put(HttpClient client, string path, object payload)
  {
    await EnsureSuccess(await client.PutAsync(path, Body(payload)), $"PUT {path}");
  }

  private static StringContent Body(object payload)
  {
    return new StringContent(JsonConvert.SerializeObject(payload, JsonSettings), Encoding.UTF8, "application/json");
  }

  /// <summary>
  /// Reports the response body. The built-in EnsureSuccessStatusCode says only "500", and these
  /// tests drive several requests in sequence against a host whose logs the runner discards.
  /// </summary>
  private static async Task EnsureSuccess(HttpResponseMessage response, string what)
  {
    Assert.True(response.IsSuccessStatusCode,
      $"{what} returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
  }
}
