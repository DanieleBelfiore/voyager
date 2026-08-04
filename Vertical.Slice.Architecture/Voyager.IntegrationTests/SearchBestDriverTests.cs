using Microsoft.Data.SqlClient;
using Voyager.TestInfra;
using Xunit;

namespace Voyager.IntegrationTests;

/// <summary>
/// Nearest-driver search against a real SQL Server. Two things sit outside a unit test's reach
/// whatever it mocks: <c>STDistance</c> returns geodetic metres — EF Core's InMemory provider has
/// no geography type, so the handler's distance threshold means nothing under it — and the spatial
/// index the query plan leans on is created by raw SQL in a migration, so it exists only if the
/// migrations really ran and the per-test reset left them alone.
/// </summary>
public class SearchBestDriverTests(VoyagerStackFixture fixture) : VoyagerIntegrationTest(fixture)
{
  [Fact]
  public async Task Search_ReturnsOnlyDriversInsideTheThreshold_MeasuredInRealMetres()
  {
    await Fixture.ResetAsync();

    var near = await Fixture.RegisterAvailableDriverAsync(DriverWorkflow.JustNorthOfRome);
    await Fixture.RegisterAvailableDriverAsync(DriverWorkflow.ADegreeNorth);

    var rider = Fixture.ClientFor(VoyagerService.Driver, await Fixture.RegisterAsync(isDriver: false));

    var response = await rider.PostAsync("api/v1/drivers/search", VoyagerJson.Content(
      new SearchBestDriverPayload { Location = DriverWorkflow.Rome, DistanceThresholdInKm = 5 })).ShouldSucceed();

    var match = Assert.Single(await response.ReadAsync<List<SearchBestDriverResult>>());

    Assert.Equal(near.Id, match.DriverId);
    // ~0.01° of latitude. The band is tight on purpose: a provider returning degrees, or planar
    // rather than geodetic metres, lands nowhere near it.
    Assert.InRange(match.Distance, 1100, 1120);
  }

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
}
