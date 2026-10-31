using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSyncAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanAddItemsFromBranch",
                table: "OrderSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanAddItemsFromCallCenter",
                table: "OrderSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanVoidFromBranch",
                table: "OrderSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanVoidFromCallCenter",
                table: "OrderSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CallCenterApiUrl",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryBranchUrl",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentOrderId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ZoneBonus",
                table: "DeliveryZones",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanAddItemsFromBranch",
                table: "OrderSettings");

            migrationBuilder.DropColumn(
                name: "CanAddItemsFromCallCenter",
                table: "OrderSettings");

            migrationBuilder.DropColumn(
                name: "CanVoidFromBranch",
                table: "OrderSettings");

            migrationBuilder.DropColumn(
                name: "CanVoidFromCallCenter",
                table: "OrderSettings");

            migrationBuilder.DropColumn(
                name: "CallCenterApiUrl",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryBranchUrl",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ParentOrderId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ZoneBonus",
                table: "DeliveryZones");
        }
    }
}
