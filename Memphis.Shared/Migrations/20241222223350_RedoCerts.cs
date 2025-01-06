using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Memphis.Shared.Migrations
{
    /// <inheritdoc />
    public partial class RedoCerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoloCerts_Certifications_CertificationId",
                table: "SoloCerts");

            migrationBuilder.DropTable(
                name: "Certifications");

            migrationBuilder.DropIndex(
                name: "IX_SoloCerts_CertificationId",
                table: "SoloCerts");

            migrationBuilder.RenameColumn(
                name: "CertificationId",
                table: "SoloCerts",
                newName: "Tier2");

            migrationBuilder.AddColumn<int>(
                name: "Center",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Tier2",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Center",
                table: "SoloCerts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Center",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Tier2",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Center",
                table: "SoloCerts");

            migrationBuilder.RenameColumn(
                name: "Tier2",
                table: "SoloCerts",
                newName: "CertificationId");

            migrationBuilder.CreateTable(
                name: "Certifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RequiredRating = table.Column<int>(type: "integer", nullable: false),
                    Solo = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SoloCerts_CertificationId",
                table: "SoloCerts",
                column: "CertificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Certifications_UserId",
                table: "Certifications",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SoloCerts_Certifications_CertificationId",
                table: "SoloCerts",
                column: "CertificationId",
                principalTable: "Certifications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
