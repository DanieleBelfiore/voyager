using Common.Core.Extensions;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ride.Handlers.Migrations
{
  /// <inheritdoc />
  public partial class RideIndex : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateSpatialIndex(
            "Rides",
            "PickupLocation",
            "IX_Rides_PickupLocation"
        );

      migrationBuilder.CreateSpatialIndex(
          "Rides",
          "DropoffLocation",
          "IX_Rides_DropoffLocation"
      );

      migrationBuilder.CreateSpatialIndex(
          "Rides",
          "LastLocation",
          "IX_Rides_LastLocation"
      );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {

      migrationBuilder.DropSpatialIndex(
              "Rides",
              "IX_Rides_PickupLocation"
          );

      migrationBuilder.DropSpatialIndex(
          "Rides",
          "IX_Rides_DropoffLocation"
      );

      migrationBuilder.DropSpatialIndex(
          "Rides",
          "IX_Rides_LastLocation"
      );
    }
  }
}
