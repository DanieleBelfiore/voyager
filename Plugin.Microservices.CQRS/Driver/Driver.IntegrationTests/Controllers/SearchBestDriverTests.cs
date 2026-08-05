using System.Net.Http.Json;
using System.Text;
using Driver.Core.Dtos;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;
using Xunit;

namespace Driver.IntegrationTests.Controllers;

/// <summary>
/// Runs against a real SQL Server (Testcontainers), unlike SearchBestDriverHandlerTests —
/// this is what actually proves the STDistance-based query returns real geodetic meters and
/// filters correctly, which EF Core's InMemory provider cannot emulate.
/// </summary>
public class SearchBestDriverTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
  private static readonly JsonSerializerSettings JsonSettings = new()
  {
    Converters = { new GeometryConverter() }
  };

  [Fact]
  public async Task SearchBestDriver_ReturnsOnlyDriversWithinThreshold_WithRealGeodeticDistance()
  {
    // Arrange
    await ResetDatabaseAsync();
    await InitializeAuthenticatedClient();

    var searchPoint = new Point(12.4964, 41.9028) { SRID = 4326 }; // Rome
    var nearDriverId = Guid.NewGuid();
    var farDriverId = Guid.NewGuid();

    Context.Drivers.Add(new Handlers.Models.Driver
    {
      Id = nearDriverId,
      LastLocation = new Point(12.4964, 41.9128) { SRID = 4326 } // ~0.01 deg north, ~1112m away
    });
    Context.Drivers.Add(new Handlers.Models.Driver
    {
      Id = farDriverId,
      LastLocation = new Point(12.4964, 42.9028) { SRID = 4326 } // ~1 deg north, ~111km away
    });
    await Context.SaveChangesAsync(CancellationToken.None);

    var body = JsonConvert.SerializeObject(new SearchBestDriverRequest { Location = searchPoint, DistanceThresholdInKm = 5 }, JsonSettings);

    // Act
    var response = await Client.PostAsync("api/v1/drivers/search", new StringContent(body, Encoding.UTF8, "application/json"));

    // Assert
    response.EnsureSuccessStatusCode();
    var results = await response.Content.ReadFromJsonAsync<List<SearchBestDriverResponse>>();

    var match = Assert.Single(results!);
    Assert.Equal(nearDriverId, match.DriverId);
    Assert.InRange(match.Distance, 1100, 1120);
  }

  /// <summary>
  /// Search hands a rider the ids of every available driver within the radius, so whatever else
  /// it returns about them is disclosed in bulk — up to MaxCandidates (200) in one call. Their
  /// positions used to be declared on that payload, which let any account map the fleet from a
  /// single request. Asserted against the raw JSON rather than the typed DTO: the DTO not having
  /// the field is exactly what this guards, so deserialising into it would assert nothing.
  /// </summary>
  [Fact]
  public async Task SearchBestDriver_DoesNotDiscloseDriverPositions()
  {
    // Arrange
    await ResetDatabaseAsync();
    await InitializeAuthenticatedClient();

    var searchPoint = new Point(12.4964, 41.9028) { SRID = 4326 };
    Context.Drivers.Add(new Handlers.Models.Driver
    {
      Id = Guid.NewGuid(),
      LastLocation = new Point(12.4964, 41.9128) { SRID = 4326 }
    });
    await Context.SaveChangesAsync(CancellationToken.None);

    var request = JsonConvert.SerializeObject(new SearchBestDriverRequest { Location = searchPoint, DistanceThresholdInKm = 5 }, JsonSettings);

    // Act
    var response = await Client.PostAsync("api/v1/drivers/search", new StringContent(request, Encoding.UTF8, "application/json"));

    // Assert
    response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadAsStringAsync();

    Assert.DoesNotContain("coordinates", body, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("lastLocation", body, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("41.91", body);
  }
}
