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
  /// Asserts 404, not merely "not 401". A token Driver cannot validate produces 500, which any
  /// "not unauthorised" assertion would happily accept — that is exactly how the cross-host
  /// lookup being broken went unnoticed here the first time. 404 means the request got past
  /// authentication and authorisation and into the handler, which then found no such driver.
  /// </summary>
  [Fact]
  public async Task Driver_AcceptsATokenIdentityIssued()
  {
    var user = await Fixture.RegisterAsync(isDriver: true);
    var client = Fixture.ClientFor(VoyagerService.Driver, user);

    var response = await client.GetAsync($"api/v1/drivers/{Guid.NewGuid()}");

    Assert.True(response.StatusCode == HttpStatusCode.NotFound,
      $"Expected 404 for an unknown driver, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
  }

  /// <summary>
  /// "RequireDriver" gates driver self-registration on the <c>is_driver</c> claim Identity stamps
  /// onto the token. It is a claim that crosses a service boundary, so both ends have to agree on
  /// it — a rider slipping through would enter the matching pool and be offered rides they cannot
  /// accept.
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
}
