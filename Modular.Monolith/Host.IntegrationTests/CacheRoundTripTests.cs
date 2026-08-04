using NetTopologySuite.Geometries;
using Voyager.TestInfra;
using Xunit;

namespace Voyager.Host.IntegrationTests;

/// <summary>
/// The driver-status cache is the one place in the system that serialises a NetTopologySuite
/// <c>Point</c> with System.Text.Json, and plain STJ cannot write one — <c>Z</c>/<c>M</c> are
/// <c>NaN</c>. That threw on every write, and the failure mode was silence: the request still
/// succeeded, the handler still returned the right answer, only from SQL every single time.
/// No unit test catches this. The endpoint behaves identically whether the cache works or not,
/// so the only witness is the store itself.
/// </summary>
public class CacheRoundTripTests(VoyagerAppFixture fixture) : VoyagerIntegrationTest(fixture)
{
  private static readonly Point Rome = new(12.4964, 41.9028) { SRID = 4326 };

  [Fact]
  public async Task ReadingADriverStatus_WritesItToRedis_WithTheGeometryIntact()
  {
    await Fixture.ResetAsync();

    var client = Fixture.NewClient();
    var driver = await VoyagerAuth.RegisterAsync(client, isDriver: true);
    client.Authenticate(driver);

    await client.PostAsync("api/v1/drivers", VoyagerJson.Content(new { })).ShouldSucceed();
    await client.PutAsync("api/v1/drivers/location", VoyagerJson.Content(new { Location = Rome })).ShouldSucceed();

    await client.GetAsync($"api/v1/drivers/{driver.Id}").ShouldSucceed();

    var cached = await Fixture.Containers.ReadCacheEntry($"driver:status:{driver.Id}");

    Assert.NotNull(cached);
    Assert.Contains("12.4964", cached);
  }
}
