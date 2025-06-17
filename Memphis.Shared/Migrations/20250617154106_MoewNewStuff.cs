using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Memphis.Shared.Migrations
{
    /// <inheritdoc />
    public partial class MoewNewStuff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confidential",
                table: "Comments");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Timetstamp",
                table: "StaffingRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Timetstamp",
                table: "StaffingRequests");

            migrationBuilder.AddColumn<bool>(
                name: "Confidential",
                table: "Comments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
