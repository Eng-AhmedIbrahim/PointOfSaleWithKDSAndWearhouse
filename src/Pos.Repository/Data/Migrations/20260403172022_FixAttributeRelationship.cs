using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixAttributeRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT name FROM sys.indexes WHERE name = 'IX_MenuSalesItems_AttributeId' AND object_id = OBJECT_ID('MenuSalesItems'))
                BEGIN
                    DROP INDEX IX_MenuSalesItems_AttributeId ON MenuSalesItems;
                END
            ");

            migrationBuilder.CreateIndex(
                name: "IX_MenuSalesItems_AttributeId",
                table: "MenuSalesItems",
                column: "AttributeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT name FROM sys.indexes WHERE name = 'IX_MenuSalesItems_AttributeId' AND object_id = OBJECT_ID('MenuSalesItems'))
                BEGIN
                    DROP INDEX IX_MenuSalesItems_AttributeId ON MenuSalesItems;
                END
            ");

            migrationBuilder.CreateIndex(
                name: "IX_MenuSalesItems_AttributeId",
                table: "MenuSalesItems",
                column: "AttributeId",
                unique: true,
                filter: "[AttributeId] IS NOT NULL");
        }
    }
}
