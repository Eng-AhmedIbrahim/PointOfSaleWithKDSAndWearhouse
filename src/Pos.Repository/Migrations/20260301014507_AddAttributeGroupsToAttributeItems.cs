using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddAttributeGroupsToAttributeItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttributeGroupId",
                table: "AttributeItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttributeGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArabicName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnglishName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    AttributeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributeGroups_Attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "Attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AlterColumn<int>(
                name: "MainCategoryId",
                table: "MenuSalesItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuSalesItems_MainCategoryId",
                table: "MenuSalesItems",
                column: "MainCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeItems_AttributeGroupId",
                table: "AttributeItems",
                column: "AttributeGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeGroups_AttributeId",
                table: "AttributeGroups",
                column: "AttributeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeItems_AttributeGroups_AttributeGroupId",
                table: "AttributeItems",
                column: "AttributeGroupId",
                principalTable: "AttributeGroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttributeItems_AttributeGroups_AttributeGroupId",
                table: "AttributeItems");

            migrationBuilder.DropTable(
                name: "AttributeGroups");

            migrationBuilder.DropIndex(
                name: "IX_MenuSalesItems_MainCategoryId",
                table: "MenuSalesItems");

            migrationBuilder.DropIndex(
                name: "IX_AttributeItems_AttributeGroupId",
                table: "AttributeItems");

            migrationBuilder.DropColumn(
                name: "AttributeGroupId",
                table: "AttributeItems");
        }
    }
}
