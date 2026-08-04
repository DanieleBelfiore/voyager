using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Driver.IntegrationTests;

/// <summary>
/// The per-test reset has to leave the database as the migrations built it. Anything that
/// rebuilds the schema from the EF model instead — <c>EnsureCreated</c> — silently drops
/// everything a migration does outside the model, and the geospatial index is exactly that:
/// raw SQL in <c>20260731180239_AddDriverLastLocationSpatialIndex</c>. Without this test the
/// suite happily proved <c>STDistance</c> against an unindexed table while claiming to cover
/// the real query plan.
/// </summary>
public class DatabaseResetTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
  [Fact]
  public async Task ResetDatabase_KeepsTheMigratedSchema_IncludingIndexesCreatedByRawSql()
  {
    await ResetDatabaseAsync();

    // Joined to sys.objects on purpose: SQL Server backs a spatial index with an internal table
    // carrying an index of the same name, so a bare sys.indexes lookup reports it twice.
    var spatialIndexes = await Context.Database
      .SqlQuery<string>($"""
                         SELECT i.name AS Value
                         FROM sys.spatial_indexes i
                         JOIN sys.objects o ON o.object_id = i.object_id
                         WHERE o.name = 'Drivers'
                         """)
      .ToListAsync();

    Assert.Contains("SIX_Drivers_LastLocation", spatialIndexes);
  }
}
