using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRecipyModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemTypeId",
                table: "MenuSalesItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemTypeCode",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemTypeId",
                table: "InventoryItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInventory",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ItemTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArabicName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnglishName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuSalesItems_ItemTypeId",
                table: "MenuSalesItems",
                column: "ItemTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuSalesItems_ItemTypes_ItemTypeId",
                table: "MenuSalesItems",
                column: "ItemTypeId",
                principalTable: "ItemTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuSalesItems_ItemTypes_ItemTypeId",
                table: "MenuSalesItems");

            migrationBuilder.DropTable(
                name: "ItemTypes");

            migrationBuilder.DropIndex(
                name: "IX_MenuSalesItems_ItemTypeId",
                table: "MenuSalesItems");

            migrationBuilder.DropColumn(
                name: "ItemTypeId",
                table: "MenuSalesItems");

            migrationBuilder.DropColumn(
                name: "ItemTypeCode",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ItemTypeId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "IsInventory",
                table: "Categories");
        }
    }
}
