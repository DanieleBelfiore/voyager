using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Respawn;
using Respawn.Graph;

namespace Voyager.TestInfra;

/// <summary>
/// Per-test cleanup for the business catalogues.
/// </summary>
/// <remarks>
/// Truncation, not <c>EnsureDeleted</c> + <c>EnsureCreated</c>. Recreating from the EF model
/// silently drops everything a migration does outside that model — the geospatial index on
/// <c>Drivers.LastLocation</c> is created by raw SQL, so a suite that recreates the schema proves
/// its geospatial query against an index no deployment is missing and this one never had.
///
/// The <c>identity</c> catalogue is deliberately not resettable through here: it carries the
/// OpenIddict client registration and the Identity tables that the token endpoint needs, and
/// wiping those between tests breaks authentication rather than isolating it. Tests get their
/// isolation from unique user names instead.
/// </remarks>
public class DatabaseResetter
{
  private readonly ConcurrentDictionary<string, Task<Respawner>> _respawners = new();

  public async Task ResetAsync(string connectionString)
  {
    var respawner = await _respawners.GetOrAdd(connectionString, CreateAsync);

    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    await respawner.ResetAsync(connection);
  }

  private static async Task<Respawner> CreateAsync(string connectionString)
  {
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    return await Respawner.CreateAsync(connection, new RespawnerOptions
    {
      DbAdapter = DbAdapter.SqlServer,
      TablesToIgnore = [new Table("__EFMigrationsHistory")]
    });
  }
}
