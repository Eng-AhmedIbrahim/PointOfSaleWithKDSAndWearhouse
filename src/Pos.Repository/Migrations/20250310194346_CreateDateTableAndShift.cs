using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class CreateDateTableAndShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    PosDate = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    StoreDate = table.Column<DateTime>(type: "DATETIME", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShiftHandovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchID = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    WorkingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartShiftTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndShiftTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShiftNumber = table.Column<int>(type: "int", nullable: true),
                    FromCashierID = table.Column<int>(type: "int", nullable: true),
                    FromCashierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ToCashierID = table.Column<int>(type: "int", nullable: true),
                    ToCashierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftHandovers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShiftID",
                table: "Orders",
                column: "ShiftID");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ShiftHandovers_ShiftID",
                table: "Orders",
                column: "ShiftID",
                principalTable: "ShiftHandovers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ShiftHandovers_ShiftID",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "AppDates");

            migrationBuilder.DropTable(
                name: "ShiftHandovers");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShiftID",
                table: "Orders");
        }
    }
}
