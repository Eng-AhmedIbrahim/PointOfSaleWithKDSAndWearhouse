using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryComplaint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Complaints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComplaintNumber = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    CustomerPhone = table.Column<string>(type: "nvarchar(15)", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    ComplaintText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComplaintDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    ResolutionDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Complaints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Complaints_DeliveryCustomerInfo_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "DeliveryCustomerInfo",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderDate",
                table: "Orders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderID",
                table: "Orders",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Phone1",
                table: "Orders",
                column: "Phone1");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Phone2",
                table: "Orders",
                column: "Phone2");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TakeawayCustomerPhone",
                table: "Orders",
                column: "TakeawayCustomerPhone");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_CustomerId",
                table: "Complaints",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Complaints");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderDate",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderID",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Phone1",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Phone2",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TakeawayCustomerPhone",
                table: "Orders");
        }
    }
}
