using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddVoidColumnsToOrdersAndItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalVoidAmount",
                table: "OrdersDetails",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidBy",
                table: "OrdersDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidByName",
                table: "OrdersDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "OrdersDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidTime",
                table: "OrdersDetails",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalVoidAmount",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "VoidBy",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "VoidByName",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "OrdersDetails");

            migrationBuilder.DropColumn(
                name: "VoidTime",
                table: "OrdersDetails");
        }
    }
}
