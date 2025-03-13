using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Memphis.Shared.Migrations
{
    /// <inheritdoc />
    public partial class Tracon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Certification_RadarId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "RadarId",
                table: "Users",
                newName: "TraconId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_RadarId",
                table: "Users",
                newName: "IX_Users_TraconId");

            migrationBuilder.RenameColumn(
                name: "ApproachHours",
                table: "Hours",
                newName: "TraconHours");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Certification_TraconId",
                table: "Users",
                column: "TraconId",
                principalTable: "Certification",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Certification_TraconId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "TraconId",
                table: "Users",
                newName: "RadarId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_TraconId",
                table: "Users",
                newName: "IX_Users_RadarId");

            migrationBuilder.RenameColumn(
                name: "TraconHours",
                table: "Hours",
                newName: "ApproachHours");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Certification_RadarId",
                table: "Users",
                column: "RadarId",
                principalTable: "Certification",
                principalColumn: "Id");
        }
    }
}
