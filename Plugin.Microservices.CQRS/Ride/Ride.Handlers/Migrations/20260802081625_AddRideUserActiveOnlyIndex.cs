using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ride.Handlers.Migrations
{
    /// <inheritdoc />
    public partial class AddRideUserActiveOnlyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Rides_UserId_ActiveOnly",
                table: "Rides",
                column: "UserId",
                unique: true,
                filter: "[Status] IN (0, 1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rides_UserId_ActiveOnly",
                table: "Rides");
        }
    }
}
