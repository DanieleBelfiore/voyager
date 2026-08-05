using Driver.Module.Entities;
using Driver.Module.Features.SearchBestDriver;
using Microsoft.Data.SqlClient;
using NetTopologySuite.Geometries;
using Voyager.TestInfra;
using Xunit;

namespace Voyager.Host.IntegrationTests;

/// <summary>
/// Nearest-driver search against a real SQL Server. Two things here are outside a unit test's
/// reach whatever it mocks: <c>STDistance</c> returns geodetic metres — EF Core's InMemory
/// provider has no geography type at all, so the handler's distance threshold means nothing under
/// it — and the spatial index the query plan depends on is created by raw SQL in a migration, so
/// it only exists if the migrations really ran.
/// </summary>
public class SearchBestDriverTests(VoyagerAppFixture fixture) : VoyagerIntegrationTest(fixture)
{
  // Rome, and a point ~0.01° north of it: a shade over a kilometre in real metres, which is what
  // makes the 5 km threshold below discriminate rather than accept everything.
  private static readonly Point Rome = new(12.4964, 41.9028) { SRID = 4326 };
  private static readonly Point JustNorthOfRome = new(12.4964, 41.9128) { SRID = 4326 };
  private static readonly Point ADegreeNorth = new(12.4964, 42.9028) { SRID = 4326 };

  [Fact]
  public async Task Search_ReturnsOnlyDriversInsideTheThreshold_MeasuredInRealMetres()
  {
    await Fixture.ResetAsync();

    var near = await RegisterAvailableDriverAt(JustNorthOfRome);
    await RegisterAvailableDriverAt(ADegreeNorth);

    var rider = await Fixture.NewAuthenticatedClientAsync(isDriver: false);
    var response = await rider.PostAsync("api/v1/drivers/search",
      VoyagerJson.Content(new SearchBestDriverRequest { Location = Rome, DistanceThresholdInKm = 5 })).ShouldSucceed();

    var matches = await response.ReadAsync<List<SearchBestDriverResponse>>();

    var match = Assert.Single(matches);
    Assert.Equal(near, match.DriverId);
    Assert.InRange(match.Distance, 1100, 1120);
  }


  /// <summary>
  /// Search hands a rider the ids of every available driver within the radius, so whatever else
  /// it returns about them is disclosed in bulk — up to MaxCandidates (200) in one call. Their
  /// positions used to be in that payload, which let any account map the fleet from a single
  /// request. Asserted against the raw JSON rather than the typed DTO: the DTO not having the
  /// field is exactly what this guards, so deserialising into it would assert nothing.
  /// </summary>
  [Fact]
  public async Task Search_DoesNotDiscloseDriverPositions()
  {
    await Fixture.ResetAsync();

    await RegisterAvailableDriverAt(JustNorthOfRome);

    var rider = await Fixture.NewAuthenticatedClientAsync(isDriver: false);
    var response = await rider.PostAsync("api/v1/drivers/search",
      VoyagerJson.Content(new SearchBestDriverRequest { Location = Rome, DistanceThresholdInKm = 5 })).ShouldSucceed();

    var body = await response.Content.ReadAsStringAsync();

    Assert.DoesNotContain("coordinates", body, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("lastLocation", body, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("41.91", body);
  }

  /// <summary>
  /// The index is created by <c>migrationBuilder.Sql</c>, so it exists only if migrations ran and
  /// the per-test reset left them alone. A suite that recreates the schema from the EF model
  /// instead would still pass the test above — against a table no deployment ever has.
  /// </summary>
  [Fact]
  public async Task DriverLocations_AreBackedByTheSpatialIndex_TheMigrationCreates()
  {
    await using var connection = new SqlConnection(Fixture.Containers.SqlConnectionString("driver"));
    await connection.OpenAsync();

    // Joined to sys.objects because SQL Server backs a spatial index with an internal table
    // carrying an index of the same name, which a bare sys.indexes lookup reports twice.
    await using var command = new SqlCommand(
      """
      SELECT COUNT(*)
      FROM sys.spatial_indexes i
      JOIN sys.objects o ON o.object_id = i.object_id
      WHERE o.name = 'Drivers'
      """, connection);

    Assert.True((int)await command.ExecuteScalarAsync() > 0, "Drivers.LastLocation has no spatial index.");
  }

  private async Task<Guid> RegisterAvailableDriverAt(Point location)
  {
    var client = Fixture.NewClient();
    var driver = await VoyagerAuth.RegisterAsync(client, isDriver: true);
    client.Authenticate(driver);

    await client.PostAsync("api/v1/drivers", VoyagerJson.Content(new { })).ShouldSucceed();
    await client.PutAsync("api/v1/drivers/location", VoyagerJson.Content(new { Location = location })).ShouldSucceed();
    await client.PutAsync("api/v1/drivers/availability", VoyagerJson.Content(new { Status = DriverStatus.Available })).ShouldSucceed();

    return driver.Id;
  }
}
