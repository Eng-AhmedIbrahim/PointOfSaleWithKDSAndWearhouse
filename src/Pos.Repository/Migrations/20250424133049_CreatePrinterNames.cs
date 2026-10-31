using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class CreatePrinterNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdersDetails_MenuSalesItems_MenuSalesItemsId",
                table: "OrdersDetails");

            migrationBuilder.DropIndex(
                name: "IX_OrdersDetails_MenuSalesItemsId",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "MenuSalesItemsId",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "PrinterName",
                table: "KitchenTypes");

            migrationBuilder.AddColumn<int>(
                name: "KitchenPrinterId",
                table: "KitchenTypes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KitchenPrinters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Copy1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Copy2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Copy3 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Copy4 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Copy5 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Copy6 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Copy7 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Copy8 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Copy9 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Copy10 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenPrinters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KitchenTypes_KitchenPrinterId",
                table: "KitchenTypes",
                column: "KitchenPrinterId",
                unique: true,
                filter: "[KitchenPrinterId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenTypes_KitchenPrinters_KitchenPrinterId",
                table: "KitchenTypes",
                column: "KitchenPrinterId",
                principalTable: "KitchenPrinters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KitchenTypes_KitchenPrinters_KitchenPrinterId",
                table: "KitchenTypes");

            migrationBuilder.DropTable(
                name: "KitchenPrinters");

            migrationBuilder.DropIndex(
                name: "IX_KitchenTypes_KitchenPrinterId",
                table: "KitchenTypes");

            migrationBuilder.DropColumn(
                name: "KitchenPrinterId",
                table: "KitchenTypes");

            migrationBuilder.AddColumn<int>(
                name: "MenuSalesItemsId",
                table: "OrdersDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrinterName",
                table: "KitchenTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdersDetails_MenuSalesItemsId",
                table: "OrdersDetails",
                column: "MenuSalesItemsId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdersDetails_MenuSalesItems_MenuSalesItemsId",
                table: "OrdersDetails",
                column: "MenuSalesItemsId",
                principalTable: "MenuSalesItems",
                principalColumn: "Id");
        }
    }
}
