using NetTopologySuite.Geometries;
using Xunit;

namespace Driver.IntegrationTests;

/// <summary>
/// The driver-status cache is the one place in the system that serialises a NetTopologySuite
/// <c>Point</c> with System.Text.Json, and plain STJ cannot write one — <c>Z</c>/<c>M</c> are
/// <c>NaN</c>. That threw on every write, and the failure mode was silence: the request still
/// succeeded and the handler still returned the right answer, from SQL, every single time. No
/// unit test catches it — the endpoint behaves identically whether the cache works or not, so the
/// only witness is the store itself.
/// </summary>
public class CacheRoundTripTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
  [Fact]
  public async Task ReadingADriverStatus_WritesItToRedis_WithTheGeometryIntact()
  {
    await ResetDatabaseAsync();
    var driverId = await InitializeAuthenticatedClient();

    Context.Drivers.Add(new Handlers.Models.Driver
    {
      Id = driverId,
      LastLocation = new Point(12.4964, 41.9028) { SRID = 4326 }
    });
    await Context.SaveChangesAsync(CancellationToken.None);

    var response = await Client.GetAsync($"api/v1/drivers/{driverId}");
    response.EnsureSuccessStatusCode();

    var cached = await Factory.ReadCacheEntry($"driver:status:{driverId}");

    Assert.NotNull(cached);
    Assert.Contains("12.4964", cached);
  }
}
