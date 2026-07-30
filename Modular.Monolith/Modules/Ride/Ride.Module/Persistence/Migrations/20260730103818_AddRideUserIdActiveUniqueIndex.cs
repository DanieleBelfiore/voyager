using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ride.Module.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRideUserIdActiveUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Rides_UserId_Active_Unique",
                table: "Rides",
                column: "UserId",
                unique: true,
                filter: "[Status] IN (0, 1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rides_UserId_Active_Unique",
                table: "Rides");
        }
    }
}
