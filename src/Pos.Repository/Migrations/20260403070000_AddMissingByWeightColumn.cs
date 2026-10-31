using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    public partial class AddMissingByWeightColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'ByWeight' AND object_id = OBJECT_ID('OrdersDetails'))
                BEGIN
                    ALTER TABLE OrdersDetails ADD ByWeight BIT NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'ByWeight' AND object_id = OBJECT_ID('MenuSalesItems'))
                BEGIN
                    ALTER TABLE MenuSalesItems ADD ByWeight BIT NOT NULL DEFAULT 0;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
