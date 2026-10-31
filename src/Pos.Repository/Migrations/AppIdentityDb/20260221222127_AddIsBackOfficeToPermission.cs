using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations.AppIdentityDb
{
    /// <inheritdoc />
    public partial class AddIsBackOfficeToPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBackOffice",
                table: "Permissions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBackOffice",
                table: "Permissions");
        }
    }
}
