using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVoidPermissionsToOrderSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('StaffMealGroups', 'DailyLimit') IS NULL
                    ALTER TABLE StaffMealGroups ADD DailyLimit INT NOT NULL DEFAULT 1;
                
                IF COL_LENGTH('StaffMealGroups', 'MealLimit') IS NULL
                    ALTER TABLE StaffMealGroups ADD MealLimit INT NOT NULL DEFAULT 1;

                IF COL_LENGTH('OrderSettings', 'CanVoidFromBranch') IS NULL
                    ALTER TABLE OrderSettings ADD CanVoidFromBranch BIT NULL DEFAULT 1;

                IF COL_LENGTH('OrderSettings', 'CanVoidFromCallCenter') IS NULL
                    ALTER TABLE OrderSettings ADD CanVoidFromCallCenter BIT NULL DEFAULT 1;

                IF COL_LENGTH('OrderSettings', 'CanAddItemsFromBranch') IS NULL
                    ALTER TABLE OrderSettings ADD CanAddItemsFromBranch BIT NULL DEFAULT 1;

                IF COL_LENGTH('OrderSettings', 'CanAddItemsFromCallCenter') IS NULL
                    ALTER TABLE OrderSettings ADD CanAddItemsFromCallCenter BIT NULL DEFAULT 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('StaffMealGroups', 'DailyLimit') IS NOT NULL
                    ALTER TABLE StaffMealGroups DROP COLUMN DailyLimit;

                IF COL_LENGTH('StaffMealGroups', 'MealLimit') IS NOT NULL
                    ALTER TABLE StaffMealGroups DROP COLUMN MealLimit;

                IF COL_LENGTH('OrderSettings', 'CanVoidFromBranch') IS NOT NULL
                    ALTER TABLE OrderSettings DROP COLUMN CanVoidFromBranch;

                IF COL_LENGTH('OrderSettings', 'CanVoidFromCallCenter') IS NOT NULL
                    ALTER TABLE OrderSettings DROP COLUMN CanVoidFromCallCenter;

                IF COL_LENGTH('OrderSettings', 'CanAddItemsFromBranch') IS NOT NULL
                    ALTER TABLE OrderSettings DROP COLUMN CanAddItemsFromBranch;

                IF COL_LENGTH('OrderSettings', 'CanAddItemsFromCallCenter') IS NOT NULL
                    ALTER TABLE OrderSettings DROP COLUMN CanAddItemsFromCallCenter;
            ");
        }
    }
}
