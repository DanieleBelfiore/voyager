using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ride.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRideConcurrencyTokenAndActiveRideUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Rides",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

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

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Rides");
        }
    }
}
