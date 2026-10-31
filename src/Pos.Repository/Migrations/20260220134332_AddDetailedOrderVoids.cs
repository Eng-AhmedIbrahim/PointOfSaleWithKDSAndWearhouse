using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailedOrderVoids : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderVoids",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderStateAtVoid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VoidDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VoidedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VoidedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsFullVoid = table.Column<bool>(type: "bit", nullable: false),
                    SubtotalBefore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxBefore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceBefore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeliveryFeesBefore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountBefore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrandTotalBefore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubtotalAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeliveryFeesAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrandTotalAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalVoidedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderVoids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderVoids_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrderVoidItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderVoidId = table.Column<int>(type: "int", nullable: false),
                    OrderDetailId = table.Column<int>(type: "int", nullable: false),
                    QuantityBefore = table.Column<int>(type: "int", nullable: false),
                    QuantityVoided = table.Column<int>(type: "int", nullable: false),
                    QuantityAfter = table.Column<int>(type: "int", nullable: false),
                    AmountBefore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountVoided = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderVoidItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderVoidItems_OrderVoids_OrderVoidId",
                        column: x => x.OrderVoidId,
                        principalTable: "OrderVoids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderVoidItems_OrdersDetails_OrderDetailId",
                        column: x => x.OrderDetailId,
                        principalTable: "OrdersDetails",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderVoidItems_OrderDetailId",
                table: "OrderVoidItems",
                column: "OrderDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderVoidItems_OrderVoidId",
                table: "OrderVoidItems",
                column: "OrderVoidId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderVoids_OrderId",
                table: "OrderVoids",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderVoidItems");

            migrationBuilder.DropTable(
                name: "OrderVoids");
        }
    }
}
