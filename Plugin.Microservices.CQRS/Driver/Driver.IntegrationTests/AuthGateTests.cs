using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Driver.IntegrationTests;

/// <summary>
/// The auth gate is middleware: scheme selection, token validation and policy evaluation all
/// happen before anything a unit test can construct.
/// </summary>
/// <remarks>
/// One caveat specific to this variant's harness. The shipped Driver service validates tokens
/// against a remote issuer over HTTP, which a local test server cannot be, so
/// <see cref="IntegrationTestWebAppFactory"/> substitutes a local OpenIddict server and a test-only
/// token endpoint. What these tests prove is therefore the pipeline — <c>[Authorize]</c>, the
/// bearer scheme, and the <c>RequireDriver</c> policy reading the claim Identity stamps — not the
/// shipped issuer configuration. The variants that boot Identity alongside Driver cover that too.
/// </remarks>
public class AuthGateTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
  [Fact]
  public async Task ProtectedEndpoint_Rejects_AnAnonymousCaller()
  {
    var anonymous = NewAnonymousClient();

    var response = await anonymous.GetAsync($"api/v1/drivers/{Guid.NewGuid()}");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  /// <summary>
  /// Asserts 404, not merely "not 401": a token the service cannot validate produces 500, which
  /// any "not unauthorised" assertion would happily accept. 404 means the request got through
  /// authentication and authorisation and into the handler, which found no such driver.
  /// </summary>
  [Fact]
  public async Task ProtectedEndpoint_Accepts_AValidToken()
  {
    await ResetDatabaseAsync();
    await InitializeAuthenticatedClient();

    var response = await Client.GetAsync($"api/v1/drivers/{Guid.NewGuid()}");

    Assert.True(response.StatusCode == HttpStatusCode.NotFound,
      $"Expected 404 for an unknown driver, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
  }

  /// <summary>
  /// "RequireDriver" gates driver self-registration on the <c>is_driver</c> claim. A rider getting
  /// through would enter the matching pool and be offered rides they cannot accept.
  /// </summary>
  [Fact]
  public async Task DriverOnlyEndpoint_Rejects_ARiderToken()
  {
    await ResetDatabaseAsync();
    await InitializeAuthenticatedClient(isDriver: false);

    var response = await Client.PostAsJsonAsync("api/v1/drivers", new { });

    Assert.True(response.StatusCode == HttpStatusCode.Forbidden,
      $"Expected 403 for a rider token, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
  }

  /// <summary>
  /// The positive half, and not a formality: rejecting a rider proves nothing on its own if the
  /// claim never reaches the token, because then everyone is rejected and the endpoint is simply
  /// dead. Both halves together are what pin the policy.
  /// </summary>
  [Fact]
  public async Task DriverOnlyEndpoint_Accepts_ADriverToken()
  {
    await ResetDatabaseAsync();
    await InitializeAuthenticatedClient(isDriver: true);

    var response = await Client.PostAsJsonAsync("api/v1/drivers", new { });

    Assert.True(response.IsSuccessStatusCode,
      $"Expected success for a driver token, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
  }
}
