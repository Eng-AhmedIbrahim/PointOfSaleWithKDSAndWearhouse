using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddDineInOrderAndTracks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DineInOrderId",
                table: "OrdersDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VoidBy",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DiscountBy",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DineInOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShiftId = table.Column<int>(type: "int", nullable: true),
                    CashierId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CashierName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CaptainId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CaptainName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TableId = table.Column<int>(type: "int", nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OrderDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrderState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "Open"),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Service = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    DiscountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DiscountReason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TotalDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true, defaultValue: "Cash"),
                    Paid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Remain = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OrderNotice = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CustomerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ClosedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrintCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DineInOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderTracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActionDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TableId = table.Column<int>(type: "int", nullable: true),
                    TableName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTracks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdersDetails_DineInOrderId",
                table: "OrdersDetails",
                column: "DineInOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DineInOrders_OrderDateTime",
                table: "DineInOrders",
                column: "OrderDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_DineInOrders_OrderId",
                table: "DineInOrders",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DineInOrders_TableId",
                table: "DineInOrders",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_DineInOrders_TableId_OrderState",
                table: "DineInOrders",
                columns: new[] { "TableId", "OrderState" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderTracks_ActionDateTime",
                table: "OrderTracks",
                column: "ActionDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTracks_OrderId",
                table: "OrderTracks",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdersDetails_DineInOrders_DineInOrderId",
                table: "OrdersDetails",
                column: "DineInOrderId",
                principalTable: "DineInOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdersDetails_DineInOrders_DineInOrderId",
                table: "OrdersDetails");

            migrationBuilder.DropTable(
                name: "DineInOrders");

            migrationBuilder.DropTable(
                name: "OrderTracks");

            migrationBuilder.DropIndex(
                name: "IX_OrdersDetails_DineInOrderId",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "DineInOrderId",
                table: "OrdersDetails");

            migrationBuilder.AlterColumn<int>(
                name: "VoidBy",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DiscountBy",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
