using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Memphis.API.Migrations
{
    /// <inheritdoc />
    public partial class TrainingSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ots_TrainingRequests_TrainingRequestId",
                table: "Ots");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTickets_TrainingRequests_TrainingRequestId",
                table: "TrainingTickets");

            migrationBuilder.DropTable(
                name: "TrainingRequests");

            migrationBuilder.DropIndex(
                name: "IX_TrainingTickets_TrainingRequestId",
                table: "TrainingTickets");

            migrationBuilder.DropIndex(
                name: "IX_Ots_TrainingRequestId",
                table: "Ots");

            migrationBuilder.DropColumn(
                name: "TrainingRequestId",
                table: "TrainingTickets");

            migrationBuilder.DropColumn(
                name: "TrainingRequestId",
                table: "Ots");

            migrationBuilder.AddColumn<string>(
                name: "DiscordId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "TowerHours",
                table: "Hours",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "GroundHours",
                table: "Hours",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "DeliveryHours",
                table: "Hours",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "CenterHours",
                table: "Hours",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "ApproachHours",
                table: "Hours",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.CreateTable(
                name: "TrainingTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<int>(type: "integer", nullable: true),
                    TypeId = table.Column<int>(type: "integer", nullable: false),
                    SelectedTypeId = table.Column<int>(type: "integer", nullable: true),
                    Start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingSchedules_TrainingTypes_SelectedTypeId",
                        column: x => x.SelectedTypeId,
                        principalTable: "TrainingTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingSchedules_TrainingTypes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "TrainingTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingSchedules_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingSchedules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSchedules_SelectedTypeId",
                table: "TrainingSchedules",
                column: "SelectedTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSchedules_StudentId",
                table: "TrainingSchedules",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSchedules_TypeId",
                table: "TrainingSchedules",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSchedules_UserId",
                table: "TrainingSchedules",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingSchedules");

            migrationBuilder.DropTable(
                name: "TrainingTypes");

            migrationBuilder.DropColumn(
                name: "DiscordId",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "TrainingRequestId",
                table: "TrainingTickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TrainingRequestId",
                table: "Ots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<float>(
                name: "TowerHours",
                table: "Hours",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<float>(
                name: "GroundHours",
                table: "Hours",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<float>(
                name: "DeliveryHours",
                table: "Hours",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<float>(
                name: "CenterHours",
                table: "Hours",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<float>(
                name: "ApproachHours",
                table: "Hours",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.CreateTable(
                name: "TrainingRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MilestoneId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    End = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Ots = table.Column<bool>(type: "boolean", nullable: false),
                    Start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingRequests_TrainingMilestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "TrainingMilestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTickets_TrainingRequestId",
                table: "TrainingTickets",
                column: "TrainingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Ots_TrainingRequestId",
                table: "Ots",
                column: "TrainingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRequests_MilestoneId",
                table: "TrainingRequests",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRequests_Start",
                table: "TrainingRequests",
                column: "Start");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRequests_Status",
                table: "TrainingRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRequests_UserId",
                table: "TrainingRequests",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ots_TrainingRequests_TrainingRequestId",
                table: "Ots",
                column: "TrainingRequestId",
                principalTable: "TrainingRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTickets_TrainingRequests_TrainingRequestId",
                table: "TrainingTickets",
                column: "TrainingRequestId",
                principalTable: "TrainingRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
