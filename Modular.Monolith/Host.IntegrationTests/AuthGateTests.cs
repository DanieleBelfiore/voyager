using System.Net;
using System.Net.Http.Json;
using Voyager.TestInfra;
using Xunit;

namespace Voyager.Host.IntegrationTests;

/// <summary>
/// The auth gate is middleware, not handler code: scheme selection, token validation and policy
/// evaluation all happen before anything a unit test can construct. This is also the only test
/// that proves the password grant works at all — every other test here depends on it.
/// </summary>
public class AuthGateTests(VoyagerAppFixture fixture) : VoyagerIntegrationTest(fixture)
{
  [Fact]
  public async Task ProtectedEndpoint_Rejects_AnAnonymousCaller()
  {
    var client = Fixture.NewClient();

    var response = await client.GetAsync($"api/v1/drivers/{Guid.NewGuid()}");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  /// <summary>
  /// Asserts an exact 200 rather than "not 401": a 403 or a 500 both satisfy "not unauthorised"
  /// while proving nothing about the request reaching the handler. Registering with
  /// <c>isDriver: true</c> makes the Identity module create the driver record, so a driver reading
  /// their own id covers the whole path — token validated, ownership check passed, record returned.
  /// </summary>
  [Fact]
  public async Task ProtectedEndpoint_Accepts_ATokenIssuedByTheIdentityModule()
  {
    var client = Fixture.NewClient();
    var user = await VoyagerAuth.RegisterAsync(client, isDriver: true);
    client.Authenticate(user);

    var response = await client.GetAsync($"api/v1/drivers/{user.Id}");

    Assert.True(response.StatusCode == HttpStatusCode.OK,
      $"Expected 200 reading own driver record, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
  }

  /// <summary>
  /// A driver's live position is self-read only. Authentication alone used to be the whole gate
  /// here, so any account could walk driver ids and track the fleet in real time; the ids are not
  /// secret either, since driver search hands them out.
  /// </summary>
  [Fact]
  public async Task ProtectedEndpoint_Rejects_ACallerReadingAnotherDriversPosition()
  {
    await Fixture.ResetAsync();

    var driver = Fixture.NewClient();
    var driverUser = await VoyagerAuth.RegisterAsync(driver, isDriver: true);
    driver.Authenticate(driverUser);

    var snooper = Fixture.NewClient();
    var riderUser = await VoyagerAuth.RegisterAsync(snooper, isDriver: false);
    snooper.Authenticate(riderUser);

    var response = await snooper.GetAsync($"api/v1/drivers/{driverUser.Id}");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  /// <summary>
  /// "RequireDriver" gates driver self-registration on the <c>is_driver</c> claim. A rider token
  /// getting through would put that account into the matching pool and have it offered rides it
  /// cannot accept. The claim marks the account type chosen at registration, not a privilege
  /// granted by anyone: registering as a driver is self-service, so this separates the rider and
  /// driver flows rather than keeping anyone out.
  /// </summary>
  [Fact]
  public async Task DriverOnlyEndpoint_Rejects_ARiderToken()
  {
    var client = await Fixture.NewAuthenticatedClientAsync(isDriver: false);

    var response = await client.PostAsJsonAsync("api/v1/drivers", new { });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  /// <summary>
  /// The same policy guards the other half of the driver flow, and this is the endpoint the
  /// host's comment names as the only one that needs it. Policy evaluation happens before the
  /// handler, so a ride id that does not exist still exercises the gate — a 404 here would mean
  /// the rider token got past it.
  /// </summary>
  [Fact]
  public async Task AcceptRide_Rejects_ARiderToken()
  {
    var client = await Fixture.NewAuthenticatedClientAsync(isDriver: false);

    var response = await client.PutAsJsonAsync($"api/v1/rides/{Guid.NewGuid()}/accept", new { });

    Assert.True(response.StatusCode == HttpStatusCode.Forbidden,
      $"Expected 403 for a rider token on AcceptRide, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
  }
}
