using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Ride.Handlers.Migrations
{
    /// <inheritdoc />
    public partial class RideStartup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Price = table.Column<double>(type: "float", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PickupLocation = table.Column<Point>(type: "geography", nullable: true),
                    PickupLocationGeoJSON = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DropoffLocation = table.Column<Point>(type: "geography", nullable: true),
                    DropoffLocationGeoJSON = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastLocation = table.Column<Point>(type: "geography", nullable: true),
                    LastLocationGeoJSON = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastUpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rides", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rides_DriverId_Status_RequestedAt",
                table: "Rides",
                columns: new[] { "DriverId", "Status", "RequestedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Rides_Status_UserId_DriverId",
                table: "Rides",
                columns: new[] { "Status", "UserId", "DriverId" });

            migrationBuilder.CreateIndex(
                name: "IX_Rides_UserId_Status_RequestedAt",
                table: "Rides",
                columns: new[] { "UserId", "Status", "RequestedAt" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rides");
        }
    }
}
