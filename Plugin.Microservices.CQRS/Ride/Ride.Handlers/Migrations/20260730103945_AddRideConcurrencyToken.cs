using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ride.Handlers.Migrations
{
  /// <inheritdoc />
  public partial class AddRideConcurrencyToken : Migration
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
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "RowVersion",
          table: "Rides");
    }
  }
}
