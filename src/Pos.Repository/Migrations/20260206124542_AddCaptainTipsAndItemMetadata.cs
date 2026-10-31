using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCaptainTipsAndItemMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CaptainTipsAmount",
                table: "OrderSettings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeductCaptainTips",
                table: "OrderSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "OrdersDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "OrdersDetails",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "OrdersDetails",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemNameAr",
                table: "OrdersDetails",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "OrdersDetails",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CaptainTipsDeduction",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MachineName",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderItemComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderItemDetailId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CommentTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemComments_OrdersDetails_OrderItemDetailId",
                        column: x => x.OrderItemDetailId,
                        principalTable: "OrdersDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemComments_OrderItemDetailId",
                table: "OrderItemComments",
                column: "OrderItemDetailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemComments");

            migrationBuilder.DropColumn(
                name: "CaptainTipsAmount",
                table: "OrderSettings");

            migrationBuilder.DropColumn(
                name: "DeductCaptainTips",
                table: "OrderSettings");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "ItemNameAr",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "CaptainTipsDeduction",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "MachineName",
                table: "Orders");
        }
    }
}
