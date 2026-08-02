using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ride.Handlers.Migrations
{
    /// <inheritdoc />
    public partial class AddRideRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DriverRating",
                table: "Rides",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiderRating",
                table: "Rides",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverRating",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "RiderRating",
                table: "Rides");
        }
    }
}
