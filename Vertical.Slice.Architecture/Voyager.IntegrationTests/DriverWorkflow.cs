using NetTopologySuite.Geometries;
using Voyager.TestInfra;

namespace Voyager.IntegrationTests;

/// <summary>
/// The sequence of calls that puts a driver into the matching pool. Shared because four of the
/// six concerns need a driver that exists, has published a location, and is marked available —
/// and every one of those is a separate endpoint on the Driver service.
/// </summary>
public static class DriverWorkflow
{
  public static readonly Point Rome = new(12.4964, 41.9028) { SRID = 4326 };
  public static readonly Point JustNorthOfRome = new(12.4964, 41.9128) { SRID = 4326 };
  public static readonly Point ADegreeNorth = new(12.4964, 42.9028) { SRID = 4326 };
  public static readonly Point AcrossTown = new(12.5164, 41.9228) { SRID = 4326 };

  public static async Task<VoyagerTestUser> RegisterAvailableDriverAsync(
    this VoyagerStackFixture fixture, Point location)
  {
    var user = await fixture.RegisterAsync(isDriver: true);
    var client = fixture.ClientFor(VoyagerService.Driver, user);

    await client.PostAsync("api/v1/drivers", VoyagerJson.Content(new { })).ShouldSucceed();
    await client.PutAsync("api/v1/drivers/location", VoyagerJson.Content(new { Location = location })).ShouldSucceed();
    await client.PutAsync("api/v1/drivers/availability",
      VoyagerJson.Content(new { Status = DriverAvailability.Available })).ShouldSucceed();

    return user;
  }
}
