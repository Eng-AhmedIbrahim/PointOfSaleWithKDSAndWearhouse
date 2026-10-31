using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatcherConfigMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DispatcherSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RefreshTimeForDeliveryOrderColorsPerSecond = table.Column<int>(type: "int", nullable: false),
                    CriticalTimeForDeliveryOrderPerMinute = table.Column<int>(type: "int", nullable: false),
                    WarningTimeForDeliveryOrderPerMinute = table.Column<int>(type: "int", nullable: false),
                    VoidLimitMinutesForDeliveryOrder = table.Column<int>(type: "int", nullable: false),
                    IsDispatcher = table.Column<bool>(type: "bit", nullable: false),
                    AllowVoidLimitMinutesForDeliveryOrder = table.Column<bool>(type: "bit", nullable: false),
                    AllowDeliveryVoidFromBranch = table.Column<bool>(type: "bit", nullable: false),
                    ComputerName = table.Column<string>(type: "nvarchar(350)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatcherSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DispatcherSettings");
        }
    }
}
