using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Memphis.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AirportMetarUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlightRules",
                table: "Airports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "Airports",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlightRules",
                table: "Airports");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Airports");
        }
    }
}
