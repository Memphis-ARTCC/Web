using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Memphis.API.Migrations
{
    /// <inheritdoc />
    public partial class OnlineControllersStuff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Cid",
                table: "OnlineControllers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "OnlineControllers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastUpdated",
                value: new DateTimeOffset(new DateTime(2024, 4, 13, 14, 41, 56, 263, DateTimeKind.Unspecified).AddTicks(453), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cid",
                table: "OnlineControllers");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "OnlineControllers");

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastUpdated",
                value: new DateTimeOffset(new DateTime(2024, 4, 13, 4, 25, 56, 546, DateTimeKind.Unspecified).AddTicks(2855), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
