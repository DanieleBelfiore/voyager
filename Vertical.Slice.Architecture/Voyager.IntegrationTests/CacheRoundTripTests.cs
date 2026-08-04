using Voyager.TestInfra;
using Xunit;

namespace Voyager.IntegrationTests;

/// <summary>
/// The driver-status cache is the one place that serialises a NetTopologySuite <c>Point</c> with
/// System.Text.Json, and plain STJ cannot write one — <c>Z</c>/<c>M</c> are <c>NaN</c>. That threw
/// on every write, and the failure mode was silence: the request still succeeded and the handler
/// still returned the right answer, from SQL, every single time. No unit test catches it — the
/// endpoint behaves identically whether the cache works or not, so the only witness is the store.
/// </summary>
public class CacheRoundTripTests(VoyagerStackFixture fixture) : VoyagerIntegrationTest(fixture)
{
  [Fact]
  public async Task ReadingADriverStatus_WritesItToRedis_WithTheGeometryIntact()
  {
    await Fixture.ResetAsync();

    var driver = await Fixture.RegisterAvailableDriverAsync(DriverWorkflow.Rome);
    var client = Fixture.ClientFor(VoyagerService.Driver, driver);

    await client.GetAsync($"api/v1/drivers/{driver.Id}").ShouldSucceed();

    var cached = await Fixture.Containers.ReadCacheEntry($"driver:status:{driver.Id}");

    Assert.NotNull(cached);
    Assert.Contains("12.4964", cached);
  }
}
