using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations.AppIdentityDb
{
    /// <inheritdoc />
    public partial class AddPermissionDetailColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL to handle renames robustly and avoid ambiguity errors
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE name = 'NameEn' AND object_id = OBJECT_ID('Permissions'))
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'ScreenEnglishName' AND object_id = OBJECT_ID('Permissions'))
                    BEGIN
                        EXEC sp_rename 'Permissions.NameEn', 'ScreenEnglishName', 'COLUMN';
                    END
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE name = 'NameAr' AND object_id = OBJECT_ID('Permissions'))
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'ScreenArabicName' AND object_id = OBJECT_ID('Permissions'))
                    BEGIN
                        EXEC sp_rename 'Permissions.NameAr', 'ScreenArabicName', 'COLUMN';
                    END
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'PoliceArabicName' AND object_id = OBJECT_ID('Permissions'))
                BEGIN
                    ALTER TABLE Permissions ADD PoliceArabicName nvarchar(max) NOT NULL DEFAULT '';
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'PoliceEnglishNameEn' AND object_id = OBJECT_ID('Permissions'))
                BEGIN
                    ALTER TABLE Permissions ADD PoliceEnglishNameEn nvarchar(max) NOT NULL DEFAULT '';
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PoliceArabicName",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "PoliceEnglishNameEn",
                table: "Permissions");

            migrationBuilder.RenameColumn(
                name: "ScreenEnglishName",
                table: "Permissions",
                newName: "NameEn");

            migrationBuilder.RenameColumn(
                name: "ScreenArabicName",
                table: "Permissions",
                newName: "NameAr");
        }
    }
}
