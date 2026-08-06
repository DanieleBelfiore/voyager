using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Voyager.TestInfra;

/// <summary>
/// Per-test cleanup for the business catalogues.
/// </summary>
/// <remarks>
/// Emptying the tables, not <c>EnsureDeleted</c> + <c>EnsureCreated</c>. Recreating from the EF model
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
  private readonly ConcurrentDictionary<string, Task<string>> _scripts = new();

  public async Task ResetAsync(string connectionString)
  {
    var script = await _scripts.GetOrAdd(connectionString, BuildScriptAsync);

    if (script.Length == 0)
      return;

    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = script;

    await command.ExecuteNonQueryAsync();
  }

  /// <summary>
  /// DELETE with the foreign keys disabled, rather than TRUNCATE or a delete order derived from
  /// the FK graph: TRUNCATE is rejected on any table a foreign key points at, disabled or not.
  /// Re-enabling is <c>WITH CHECK</c> so the constraints go back trusted — free on tables that
  /// were just emptied, and leaving them untrusted would change the query plans the rest of the
  /// suite measures. The schema is fixed once the migrations have run, so the script is built
  /// once per catalogue and replayed.
  /// </summary>
  private static async Task<string> BuildScriptAsync(string connectionString)
  {
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = """
                          SELECT QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name)
                          FROM sys.tables
                          WHERE is_ms_shipped = 0 AND name <> '__EFMigrationsHistory'
                          """;

    var tables = new List<string>();

    await using (var reader = await command.ExecuteReaderAsync())
    {
      while (await reader.ReadAsync())
        tables.Add(reader.GetString(0));
    }

    if (tables.Count == 0)
      return string.Empty;

    var script = new StringBuilder();

    foreach (var table in tables)
      script.AppendLine($"ALTER TABLE {table} NOCHECK CONSTRAINT ALL;");

    foreach (var table in tables)
      script.AppendLine($"DELETE FROM {table};");

    foreach (var table in tables)
      script.AppendLine($"ALTER TABLE {table} WITH CHECK CHECK CONSTRAINT ALL;");

    return script.ToString();
  }
}
