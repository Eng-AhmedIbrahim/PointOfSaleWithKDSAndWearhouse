using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenPropertiesToOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryKitchenTypeId",
                table: "OrdersDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemKitchenTypeId",
                table: "OrdersDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrintInBackupReceiptFromCategory",
                table: "OrdersDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrintInBackupReceiptFromItem",
                table: "OrdersDetails",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryKitchenTypeId",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "ItemKitchenTypeId",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "PrintInBackupReceiptFromCategory",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "PrintInBackupReceiptFromItem",
                table: "OrdersDetails");
        }
    }
}
