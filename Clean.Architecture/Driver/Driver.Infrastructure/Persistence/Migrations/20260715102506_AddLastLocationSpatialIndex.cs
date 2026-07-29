using Microsoft.EntityFrameworkCore.Migrations;
using Voyager.Shared.Extensions;

#nullable disable

namespace Driver.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLastLocationSpatialIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSpatialIndex(
                "Drivers",
                "LastLocation",
                "IX_Drivers_LastLocation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSpatialIndex(
                "Drivers",
                "IX_Drivers_LastLocation");
        }
    }
}
