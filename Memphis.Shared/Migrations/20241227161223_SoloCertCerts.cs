using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Memphis.Shared.Migrations
{
    /// <inheritdoc />
    public partial class SoloCertCerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Certification");

            migrationBuilder.AddColumn<bool>(
                name: "Solo",
                table: "Certification",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Solo",
                table: "Certification");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Certification",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
