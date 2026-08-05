using System.Net;
using System.Net.Http.Json;
using Voyager.TestInfra;
using Xunit;

namespace Voyager.IntegrationTests;

/// <summary>
/// A token issued by Identity, accepted by Driver — two separate hosts.
/// </summary>
/// <remarks>
/// This is the variant's real auth topology and none of it exists in-process: Identity signs the
/// token, Driver validates it by fetching the issuer's discovery document and signing keys over
/// HTTP. Nothing short of two running hosts exercises that, which is why every other test in this
/// assembly depends on this one holding.
/// </remarks>
public class AuthGateTests(VoyagerStackFixture fixture) : VoyagerIntegrationTest(fixture)
{
  [Fact]
  public async Task Driver_RejectsAnAnonymousCaller()
  {
    var client = Fixture.ClientFor(VoyagerService.Driver);

    var response = await client.GetAsync($"api/v1/drivers/{Guid.NewGuid()}");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  /// <summary>
  /// Asserts an exact 200, not merely "not 401". A token Driver cannot validate produces 500,
  /// which any "not unauthorised" assertion would happily accept — that is exactly how the
  /// cross-host lookup being broken went unnoticed here the first time. Registering with
  /// <c>isDriver: true</c> makes Identity create the driver record, so a driver reading their own
  /// id exercises the whole path: token validated, ownership check passed, record returned.
  /// </summary>
  [Fact]
  public async Task Driver_AcceptsATokenIdentityIssued()
  {
    var user = await Fixture.RegisterAsync(isDriver: true);
    var client = Fixture.ClientFor(VoyagerService.Driver, user);

    var response = await client.GetAsync($"api/v1/drivers/{user.Id}");

    Assert.True(response.StatusCode == HttpStatusCode.OK,
      $"Expected 200 reading own driver record, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
  }

  /// <summary>
  /// A driver's live position is self-read only. Authentication alone used to be the whole gate
  /// here, so any account could walk driver ids and track the fleet in real time; the ids are not
  /// secret either, since driver search hands them out. 403 rather than 404 because the caller is
  /// authenticated and the resource exists — they simply do not own it.
  /// </summary>
  [Fact]
  public async Task Driver_RejectsACallerReadingAnotherDriversPosition()
  {
    await Fixture.ResetAsync();

    var target = await Fixture.RegisterAvailableDriverAsync(DriverWorkflow.Rome);
    var snooper = Fixture.ClientFor(VoyagerService.Driver, await Fixture.RegisterAsync(isDriver: false));

    var response = await snooper.GetAsync($"api/v1/drivers/{target.Id}");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  /// <summary>
  /// "RequireDriver" gates driver self-registration on the <c>is_driver</c> claim Identity stamps
  /// onto the token. It is a claim that crosses a service boundary, so both ends have to agree on
  /// its exact value — a rider slipping through would enter the matching pool and be offered rides
  /// they cannot accept. It marks the account type chosen at registration, not a privilege granted
  /// by anyone: registering as a driver is self-service, so this separates the rider and driver
  /// flows rather than keeping anyone out.
  /// </summary>
  [Fact]
  public async Task Driver_RejectsARiderToken_OnTheDriverOnlyEndpoint()
  {
    var user = await Fixture.RegisterAsync(isDriver: false);
    var client = Fixture.ClientFor(VoyagerService.Driver, user);

    var response = await client.PostAsJsonAsync("api/v1/drivers", new { });

    Assert.True(response.StatusCode == HttpStatusCode.Forbidden,
      $"Expected 403 for a rider token, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
  }

  /// <summary>
  /// The same policy guards the other half of the driver flow, and this is the endpoint the
  /// Ride host's comment names as the only one that needs it. Policy evaluation happens before
  /// the handler, so a ride id that does not exist still exercises the gate — a 404 here would
  /// mean the rider token got past it.
  /// </summary>
  [Fact]
  public async Task Ride_RejectsARiderToken_OnAcceptRide()
  {
    var user = await Fixture.RegisterAsync(isDriver: false);
    var client = Fixture.ClientFor(VoyagerService.Ride, user);

    var response = await client.PutAsJsonAsync($"api/v1/rides/{Guid.NewGuid()}/accept", new { });

    Assert.True(response.StatusCode == HttpStatusCode.Forbidden,
      $"Expected 403 for a rider token on AcceptRide, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
  }
}
