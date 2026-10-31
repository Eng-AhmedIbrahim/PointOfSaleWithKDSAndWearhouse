using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddHqSettingsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConnectTimeout",
                table: "HqSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Encrypt",
                table: "HqSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TrustServerCertificate",
                table: "HqSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConnectTimeout",
                table: "HqSettings");

            migrationBuilder.DropColumn(
                name: "Encrypt",
                table: "HqSettings");

            migrationBuilder.DropColumn(
                name: "TrustServerCertificate",
                table: "HqSettings");
        }
    }
}
