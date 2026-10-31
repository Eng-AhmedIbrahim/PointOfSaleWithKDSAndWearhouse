using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAttributePriceFromMenuItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttributePrice",
                table: "MenuSalesItems");

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraPrice",
                table: "OrderItemAttributes",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Uid",
                table: "AttributeGroups",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraPrice",
                table: "OrderItemAttributes");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "AttributeGroups");

            migrationBuilder.AddColumn<decimal>(
                name: "AttributePrice",
                table: "MenuSalesItems",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
