using Common.Core.Extensions;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Driver.Handlers.Migrations
{
  /// <inheritdoc />
  public partial class DriverIndex : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateSpatialIndex(
            "Drivers",
            "LastLocation",
            "IX_Drivers_LastLocation"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropSpatialIndex(
            "Drivers",
            "IX_Drivers_LastLocation"
        );
    }
  }
}
