using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Memphis.Shared.Migrations
{
    /// <inheritdoc />
    public partial class UserCerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificationUser");

            migrationBuilder.AddColumn<int>(
                name: "CenterId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GroundId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RadarId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TowerId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CenterId",
                table: "Users",
                column: "CenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GroundId",
                table: "Users",
                column: "GroundId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RadarId",
                table: "Users",
                column: "RadarId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TowerId",
                table: "Users",
                column: "TowerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Certification_CenterId",
                table: "Users",
                column: "CenterId",
                principalTable: "Certification",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Certification_GroundId",
                table: "Users",
                column: "GroundId",
                principalTable: "Certification",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Certification_RadarId",
                table: "Users",
                column: "RadarId",
                principalTable: "Certification",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Certification_TowerId",
                table: "Users",
                column: "TowerId",
                principalTable: "Certification",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Certification_CenterId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Certification_GroundId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Certification_RadarId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Certification_TowerId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CenterId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_GroundId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RadarId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TowerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CenterId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GroundId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RadarId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TowerId",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "CertificationUser",
                columns: table => new
                {
                    CertificationsId = table.Column<int>(type: "integer", nullable: false),
                    UsersId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationUser", x => new { x.CertificationsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_CertificationUser_Certification_CertificationsId",
                        column: x => x.CertificationsId,
                        principalTable: "Certification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificationUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificationUser_UsersId",
                table: "CertificationUser",
                column: "UsersId");
        }
    }
}
