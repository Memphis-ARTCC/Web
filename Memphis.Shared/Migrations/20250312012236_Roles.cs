using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Memphis.Shared.Migrations
{
    /// <inheritdoc />
    public partial class Roles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Email", "Name", "NameShort" },
                values: new object[] { "events@memphisartcc.com", "Events Team", "EVENTS" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Email", "Name", "NameShort" },
                values: new object[] { "socialmedia@memphisartcc.com", "Social Media Team", "SOCIAL" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Email", "Name", "NameShort" },
                values: new object[,]
                {
                    { 13, "instructors@memphisartcc.com", "Instructor", "INS" },
                    { 14, "mentors@memphisartcc.com", "Mentor", "MTR" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Email", "Name", "NameShort" },
                values: new object[] { "instructors@memphisartcc.com", "Instructor", "INS" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Email", "Name", "NameShort" },
                values: new object[] { "mentors@memphisartcc.com", "Mentor", "MTR" });
        }
    }
}
