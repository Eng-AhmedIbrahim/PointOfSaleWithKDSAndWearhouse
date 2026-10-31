using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdataItemInventorySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "InventoryItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryNameAr",
                table: "InventoryItems",
                type: "nvarchar(350)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryNameEn",
                table: "InventoryItems",
                type: "nvarchar(350)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemNameAr",
                table: "InventoryItems",
                type: "nvarchar(350)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemNameEn",
                table: "InventoryItems",
                type: "nvarchar(350)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitNameAr",
                table: "InventoryItems",
                type: "nvarchar(350)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitNameEn",
                table: "InventoryItems",
                type: "nvarchar(350)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "CategoryNameAr",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "CategoryNameEn",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ItemNameAr",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ItemNameEn",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "UnitNameAr",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "UnitNameEn",
                table: "InventoryItems");
        }
    }
}
