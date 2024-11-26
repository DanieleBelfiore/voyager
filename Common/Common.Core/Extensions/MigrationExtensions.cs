using Microsoft.EntityFrameworkCore.Migrations;

namespace Common.Core.Extensions
{
  public static class MigrationExtensions
  {
    public static void CreateSpatialIndex(this MigrationBuilder migrationBuilder, string tableName, string columnName, string indexName)
    {
      migrationBuilder.Sql($"""

                                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = '{indexName}' AND object_id = OBJECT_ID('{tableName}'))
                                        BEGIN
                                            CREATE SPATIAL INDEX [{indexName}] ON [{tableName}] ([{columnName}])
                                            USING GEOGRAPHY_GRID
                                            WITH (
                                                GRIDS = (
                                                    LEVEL_1 = HIGH,
                                                    LEVEL_2 = HIGH,
                                                    LEVEL_3 = HIGH,
                                                    LEVEL_4 = HIGH
                                                ),
                                                CELLS_PER_OBJECT = 64,
                                                PAD_INDEX = OFF,
                                                STATISTICS_NORECOMPUTE = OFF,
                                                SORT_IN_TEMPDB = OFF,
                                                DROP_EXISTING = OFF,
                                                ONLINE = OFF,
                                                ALLOW_ROW_LOCKS = ON,
                                                ALLOW_PAGE_LOCKS = ON
                                            )
                                        END

                            """);
    }

    public static void DropSpatialIndex(this MigrationBuilder migrationBuilder, string tableName, string indexName)
    {
      migrationBuilder.Sql($"""

                                        IF EXISTS (SELECT * FROM sys.indexes WHERE name = '{indexName}' AND object_id = OBJECT_ID('{tableName}'))
                                        BEGIN
                                            DROP INDEX [{indexName}] ON [{tableName}]
                                        END

                            """);
    }
  }
}
