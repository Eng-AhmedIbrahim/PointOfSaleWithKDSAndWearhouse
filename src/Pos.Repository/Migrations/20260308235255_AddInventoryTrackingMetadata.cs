using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryTrackingMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "InventoryTransactions",
                type: "nvarchar(600)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryTransactionImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryTransactionId = table.Column<int>(type: "int", nullable: false),
                    Base64Content = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactionImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactionImages_InventoryTransactions_InventoryTransactionId",
                        column: x => x.InventoryTransactionId,
                        principalTable: "InventoryTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactionImages_InventoryTransactionId",
                table: "InventoryTransactionImages",
                column: "InventoryTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryTransactionImages");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "InventoryTransactions");
        }
    }
}
