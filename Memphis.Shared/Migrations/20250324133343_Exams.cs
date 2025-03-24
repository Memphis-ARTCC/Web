using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Memphis.Shared.Migrations
{
    /// <inheritdoc />
    public partial class Exams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Link = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstructorId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    ExamId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamRequests_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamRequests_Users_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamRequests_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Email", "Name", "NameShort" },
                values: new object[] { "web@memphisartcc.com", "Web Team", "WEB" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Email", "Name", "NameShort" },
                values: new object[] { "events@memphisartcc.com", "Events Team", "EVENTS" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Email", "Name", "NameShort" },
                values: new object[] { "facilities@memphisartcc.com", "Facilities Team", "FACILITIES" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Email", "Name", "NameShort" },
                values: new object[] { "socialmedia@memphisartcc.com", "Social Media Team", "SOCIAL" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Email", "Name", "NameShort" },
                values: new object[,]
                {
                    { 15, "instructors@memphisartcc.com", "Instructor", "INS" },
                    { 16, "mentors@memphisartcc.com", "Mentor", "MTR" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamRequests_ExamId",
                table: "ExamRequests",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamRequests_InstructorId",
                table: "ExamRequests",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamRequests_StudentId",
                table: "ExamRequests",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamRequests");

            migrationBuilder.DropTable(
                name: "Exams");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 16);

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

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Email", "Name", "NameShort" },
                values: new object[] { "instructors@memphisartcc.com", "Instructor", "INS" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Email", "Name", "NameShort" },
                values: new object[] { "mentors@memphisartcc.com", "Mentor", "MTR" });
        }
    }
}
