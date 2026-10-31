using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountReasonToOrderItemsDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OrdersDetails]') AND name = 'DiscountReason')
BEGIN
    ALTER TABLE [OrdersDetails] ADD [DiscountReason] nvarchar(250) NULL;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountReason",
                table: "OrdersDetails");
        }
    }
}
