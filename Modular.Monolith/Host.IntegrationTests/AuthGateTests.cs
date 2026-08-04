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

  [Fact]
  public async Task ProtectedEndpoint_Accepts_ATokenIssuedByTheIdentityModule()
  {
    var client = await Fixture.NewAuthenticatedClientAsync(isDriver: true);

    var response = await client.GetAsync($"api/v1/drivers/{Guid.NewGuid()}");

    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  /// <summary>
  /// "RequireDriver" gates driver self-registration on the <c>is_driver</c> claim. A rider who
  /// could register as a driver would enter the matching pool and be offered rides they cannot
  /// accept, so this is an authorisation rule with teeth, not a formality.
  /// </summary>
  [Fact]
  public async Task DriverOnlyEndpoint_Rejects_ARiderToken()
  {
    var client = await Fixture.NewAuthenticatedClientAsync(isDriver: false);

    var response = await client.PostAsJsonAsync("api/v1/drivers", new { });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }
}
