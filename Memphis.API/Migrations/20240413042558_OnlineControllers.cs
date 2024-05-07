using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Memphis.API.Migrations
{
    /// <inheritdoc />
    public partial class OnlineControllers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Duration",
                table: "OnlineControllers",
                type: "text",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastUpdated",
                value: new DateTimeOffset(new DateTime(2024, 4, 13, 4, 25, 56, 546, DateTimeKind.Unspecified).AddTicks(2855), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Duration",
                table: "OnlineControllers",
                type: "interval",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastUpdated",
                value: new DateTimeOffset(new DateTime(2024, 1, 23, 3, 8, 4, 225, DateTimeKind.Unspecified).AddTicks(8735), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
