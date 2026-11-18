using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseSignatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Signature",
                table: "Licenses",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Signature",
                table: "LicensedDevices",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Signature",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "Signature",
                table: "LicensedDevices");
        }
    }
}
