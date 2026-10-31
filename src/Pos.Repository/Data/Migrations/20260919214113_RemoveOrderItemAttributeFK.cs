using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrderItemAttributeFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the FK constraint only if it exists.
            // This handles both fresh installs (FK exists) and databases where it was already dropped manually.
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_OrderItemAttributes_AttributeItems_AttributeItemId'
                    AND parent_object_id = OBJECT_ID('OrderItemAttributes')
                )
                BEGIN
                    ALTER TABLE [OrderItemAttributes] DROP CONSTRAINT [FK_OrderItemAttributes_AttributeItems_AttributeItemId];
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore FK only if it doesn't already exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_OrderItemAttributes_AttributeItems_AttributeItemId'
                    AND parent_object_id = OBJECT_ID('OrderItemAttributes')
                )
                BEGIN
                    ALTER TABLE [OrderItemAttributes]
                    ADD CONSTRAINT [FK_OrderItemAttributes_AttributeItems_AttributeItemId]
                    FOREIGN KEY ([AttributeItemId]) REFERENCES [AttributeItems] ([Id])
                    ON DELETE NO ACTION;
                END
            ");
        }

    }
}
