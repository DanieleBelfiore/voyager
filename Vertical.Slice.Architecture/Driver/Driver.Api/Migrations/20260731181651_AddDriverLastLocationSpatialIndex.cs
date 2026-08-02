using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Driver.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverLastLocationSpatialIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF Core has no fluent API for spatial indexes — hand-written SQL is the only way.
            // GEOGRAPHY_AUTO_GRID lets SQL Server pick grid density instead of hand-tuning it,
            // and is what makes SearchBestDriverHandler's STDistance filter usable by the query
            // optimizer instead of a full table scan.
            migrationBuilder.Sql(@"
                CREATE SPATIAL INDEX SIX_Drivers_LastLocation
                ON Drivers(LastLocation)
                USING GEOGRAPHY_AUTO_GRID;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX SIX_Drivers_LastLocation ON Drivers;");
        }
    }
}
