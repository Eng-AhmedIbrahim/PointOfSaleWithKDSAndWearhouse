using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class DeviceBasedPrinters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KitchenTypes_KitchenPrinters_KitchenPrinterId",
                table: "KitchenTypes");

            migrationBuilder.DropIndex(
                name: "IX_KitchenTypes_KitchenPrinterId",
                table: "KitchenTypes");

            migrationBuilder.DropColumn(
                name: "KitchenPrinterId",
                table: "KitchenTypes");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenPrinters_KitchenTypeId",
                table: "KitchenPrinters",
                column: "KitchenTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenPrinters_KitchenTypes_KitchenTypeId",
                table: "KitchenPrinters",
                column: "KitchenTypeId",
                principalTable: "KitchenTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KitchenPrinters_KitchenTypes_KitchenTypeId",
                table: "KitchenPrinters");

            migrationBuilder.DropIndex(
                name: "IX_KitchenPrinters_KitchenTypeId",
                table: "KitchenPrinters");

            migrationBuilder.AddColumn<int>(
                name: "KitchenPrinterId",
                table: "KitchenTypes",
                type: "int",
                nullable: true);

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
    }
}
