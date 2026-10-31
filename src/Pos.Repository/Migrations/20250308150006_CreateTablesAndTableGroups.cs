using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class CreateTablesAndTableGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "kitchenName",
                table: "PrintingSettings",
                newName: "OutLetType");

            migrationBuilder.CreateTable(
                name: "TableGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    BranchID = table.Column<int>(type: "int", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchID = table.Column<int>(type: "int", nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TableState = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Available"),
                    SeatsCount = table.Column<int>(type: "int", nullable: true),
                    LocationX = table.Column<int>(type: "int", nullable: true),
                    LocationY = table.Column<int>(type: "int", nullable: true),
                    SmokingSection = table.Column<bool>(type: "bit", nullable: true),
                    NearWindows = table.Column<bool>(type: "bit", nullable: true),
                    BoothSeating = table.Column<bool>(type: "bit", nullable: true),
                    PrivateSeating = table.Column<bool>(type: "bit", nullable: true),
                    LoadIndex = table.Column<int>(type: "int", nullable: true),
                    Usable = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    TableShape = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TimeStamp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GroupID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tables_TableGroups_GroupID",
                        column: x => x.GroupID,
                        principalTable: "TableGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tables_GroupID",
                table: "Tables",
                column: "GroupID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tables");

            migrationBuilder.DropTable(
                name: "TableGroups");

            migrationBuilder.RenameColumn(
                name: "OutLetType",
                table: "PrintingSettings",
                newName: "kitchenName");
        }
    }
}
