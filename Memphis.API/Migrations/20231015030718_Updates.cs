using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Memphis.API.Migrations
{
    /// <inheritdoc />
    public partial class Updates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ots_Users_InstructorId",
                table: "Ots");

            migrationBuilder.DropTable(
                name: "FeedbackCannedResponse");

            migrationBuilder.DropColumn(
                name: "Facility",
                table: "TrainingTickets");

            migrationBuilder.DropColumn(
                name: "ControllerName",
                table: "Feedback");

            migrationBuilder.RenameColumn(
                name: "Position",
                table: "TrainingTickets",
                newName: "MilestoneId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainingTickets_Position",
                table: "TrainingTickets",
                newName: "IX_TrainingTickets_MilestoneId");

            migrationBuilder.RenameColumn(
                name: "Position",
                table: "TrainingRequests",
                newName: "MilestoneId");

            migrationBuilder.RenameColumn(
                name: "Position",
                table: "Ots",
                newName: "MilestoneId");

            migrationBuilder.RenameIndex(
                name: "IX_Ots_Position",
                table: "Ots",
                newName: "IX_Ots_MilestoneId");

            migrationBuilder.AddColumn<bool>(
                name: "Ots",
                table: "TrainingRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "TrainingRequestId",
                table: "Ots",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "InstructorId",
                table: "Ots",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Link = table.Column<string>(type: "text", nullable: false),
                    Read = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingMilestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Facility = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingMilestones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRequests_MilestoneId",
                table: "TrainingRequests",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Ots_TrainingRequestId",
                table: "Ots",
                column: "TrainingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_ControllerId",
                table: "Feedback",
                column: "ControllerId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Read",
                table: "Notifications",
                column: "Read");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Title",
                table: "Notifications",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingMilestones_Code",
                table: "TrainingMilestones",
                column: "Code");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedback_Users_ControllerId",
                table: "Feedback",
                column: "ControllerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Hours_Users_UserId",
                table: "Hours",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ots_TrainingMilestones_MilestoneId",
                table: "Ots",
                column: "MilestoneId",
                principalTable: "TrainingMilestones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ots_TrainingRequests_TrainingRequestId",
                table: "Ots",
                column: "TrainingRequestId",
                principalTable: "TrainingRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ots_Users_InstructorId",
                table: "Ots",
                column: "InstructorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingRequests_TrainingMilestones_MilestoneId",
                table: "TrainingRequests",
                column: "MilestoneId",
                principalTable: "TrainingMilestones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTickets_TrainingMilestones_MilestoneId",
                table: "TrainingTickets",
                column: "MilestoneId",
                principalTable: "TrainingMilestones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedback_Users_ControllerId",
                table: "Feedback");

            migrationBuilder.DropForeignKey(
                name: "FK_Hours_Users_UserId",
                table: "Hours");

            migrationBuilder.DropForeignKey(
                name: "FK_Ots_TrainingMilestones_MilestoneId",
                table: "Ots");

            migrationBuilder.DropForeignKey(
                name: "FK_Ots_TrainingRequests_TrainingRequestId",
                table: "Ots");

            migrationBuilder.DropForeignKey(
                name: "FK_Ots_Users_InstructorId",
                table: "Ots");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingRequests_TrainingMilestones_MilestoneId",
                table: "TrainingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTickets_TrainingMilestones_MilestoneId",
                table: "TrainingTickets");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "TrainingMilestones");

            migrationBuilder.DropIndex(
                name: "IX_TrainingRequests_MilestoneId",
                table: "TrainingRequests");

            migrationBuilder.DropIndex(
                name: "IX_Ots_TrainingRequestId",
                table: "Ots");

            migrationBuilder.DropIndex(
                name: "IX_Feedback_ControllerId",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "Ots",
                table: "TrainingRequests");

            migrationBuilder.RenameColumn(
                name: "MilestoneId",
                table: "TrainingTickets",
                newName: "Position");

            migrationBuilder.RenameIndex(
                name: "IX_TrainingTickets_MilestoneId",
                table: "TrainingTickets",
                newName: "IX_TrainingTickets_Position");

            migrationBuilder.RenameColumn(
                name: "MilestoneId",
                table: "TrainingRequests",
                newName: "Position");

            migrationBuilder.RenameColumn(
                name: "MilestoneId",
                table: "Ots",
                newName: "Position");

            migrationBuilder.RenameIndex(
                name: "IX_Ots_MilestoneId",
                table: "Ots",
                newName: "IX_Ots_Position");

            migrationBuilder.AddColumn<string>(
                name: "Facility",
                table: "TrainingTickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "TrainingRequestId",
                table: "Ots",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "InstructorId",
                table: "Ots",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "ControllerName",
                table: "Feedback",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FeedbackCannedResponse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Response = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackCannedResponse", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Ots_Users_InstructorId",
                table: "Ots",
                column: "InstructorId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
