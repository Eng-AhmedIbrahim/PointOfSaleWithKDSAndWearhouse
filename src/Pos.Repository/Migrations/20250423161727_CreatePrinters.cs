using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class CreatePrinters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<int>(
                name: "DeliveryCompanyInfoId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KitchenTypeId",
                table: "MenuSalesItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrintInBackupReceipt",
                table: "MenuSalesItems",
                type: "bit",
                nullable: true,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "PrinterName",
                table: "KitchenTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KitchenTypeId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrintInBackupReceipt",
                table: "Categories",
                type: "bit",
                nullable: true,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "DeliveryCompanyInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryCompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BackTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalOrdersAmount = table.Column<int>(type: "int", nullable: true),
                    TotalOrdersCash = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryCompanyInfo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveryCompanyInfoId",
                table: "Orders",
                column: "DeliveryCompanyInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuSalesItems_KitchenTypeId",
                table: "MenuSalesItems",
                column: "KitchenTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_KitchenTypeId",
                table: "Categories",
                column: "KitchenTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_KitchenTypes_KitchenTypeId",
                table: "Categories",
                column: "KitchenTypeId",
                principalTable: "KitchenTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuSalesItems_KitchenTypes_KitchenTypeId",
                table: "MenuSalesItems",
                column: "KitchenTypeId",
                principalTable: "KitchenTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_DeliveryCompanyInfo_DeliveryCompanyInfoId",
                table: "Orders",
                column: "DeliveryCompanyInfoId",
                principalTable: "DeliveryCompanyInfo",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_KitchenTypes_KitchenTypeId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuSalesItems_KitchenTypes_KitchenTypeId",
                table: "MenuSalesItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_DeliveryCompanyInfo_DeliveryCompanyInfoId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "DeliveryCompanyInfo");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DeliveryCompanyInfoId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_MenuSalesItems_KitchenTypeId",
                table: "MenuSalesItems");

            migrationBuilder.DropIndex(
                name: "IX_Categories_KitchenTypeId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DeliveryCompanyInfoId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "KitchenTypeId",
                table: "MenuSalesItems");

            migrationBuilder.DropColumn(
                name: "PrintInBackupReceipt",
                table: "MenuSalesItems");

            migrationBuilder.DropColumn(
                name: "PrinterName",
                table: "KitchenTypes");

            migrationBuilder.DropColumn(
                name: "KitchenTypeId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "PrintInBackupReceipt",
                table: "Categories");

        }
    }
}
