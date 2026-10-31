using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffMealEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StaffMealConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DailyLimit = table.Column<int>(type: "int", nullable: false),
                    SpecialPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMealConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffMealConfigs_MenuSalesItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "MenuSalesItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StaffMealGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMealGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffMealUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMealUsages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffMealGroupItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMealGroupItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffMealGroupItems_MenuSalesItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "MenuSalesItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StaffMealGroupItems_StaffMealGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "StaffMealGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffMealConfigs_ItemId",
                table: "StaffMealConfigs",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffMealGroupItems_GroupId",
                table: "StaffMealGroupItems",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffMealGroupItems_ItemId",
                table: "StaffMealGroupItems",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffMealConfigs");

            migrationBuilder.DropTable(
                name: "StaffMealGroupItems");

            migrationBuilder.DropTable(
                name: "StaffMealUsages");

            migrationBuilder.DropTable(
                name: "StaffMealGroups");
        }
    }
}
