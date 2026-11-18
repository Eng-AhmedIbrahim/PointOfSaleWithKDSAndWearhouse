using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchCostPnLAndReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "InventoryTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "InventoryItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsReturn",
                table: "BranchRequisitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BranchDailyPnLSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalGrossSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDiscounts = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalVoids = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCOGS = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalWasteCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalInventoryPurchases = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrossProfit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrossProfitMargin = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalExpenses = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetProfit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderCount = table.Column<int>(type: "int", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchDailyPnLSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BranchInventoryCostBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    MenuSalesItemId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BatchSource = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsExhausted = table.Column<bool>(type: "bit", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchInventoryCostBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseId = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseItems_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionRecipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProducedProductId = table.Column<int>(type: "int", nullable: false),
                    YieldQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    YieldUnitId = table.Column<int>(type: "int", nullable: true),
                    EstimatedBatchCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionRecipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionRecipes_WarehouseProducts_ProducedProductId",
                        column: x => x.ProducedProductId,
                        principalTable: "WarehouseProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRecipes_WarehouseUnits_YieldUnitId",
                        column: x => x.YieldUnitId,
                        principalTable: "WarehouseUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PurchaseOrderId = table.Column<int>(type: "int", nullable: true),
                    GoodsReceiptNoteId = table.Column<int>(type: "int", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_GoodsReceiptNotes_GoodsReceiptNoteId",
                        column: x => x.GoodsReceiptNoteId,
                        principalTable: "GoodsReceiptNotes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierReturns_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierReturns_WH_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "WH_Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ProducedProductId = table.Column<int>(type: "int", nullable: false),
                    ProductionRecipeId = table.Column<int>(type: "int", nullable: true),
                    TargetQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualQuantityProduced = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalActualCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_ProductionRecipes_ProductionRecipeId",
                        column: x => x.ProductionRecipeId,
                        principalTable: "ProductionRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_WarehouseProducts_ProducedProductId",
                        column: x => x.ProducedProductId,
                        principalTable: "WarehouseProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionRecipeIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionRecipeId = table.Column<int>(type: "int", nullable: false),
                    IngredientProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    WastePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionRecipeIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionRecipeIngredients_ProductionRecipes_ProductionRecipeId",
                        column: x => x.ProductionRecipeId,
                        principalTable: "ProductionRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionRecipeIngredients_WarehouseProducts_IngredientProductId",
                        column: x => x.IngredientProductId,
                        principalTable: "WarehouseProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRecipeIngredients_WarehouseUnits_UnitId",
                        column: x => x.UnitId,
                        principalTable: "WarehouseUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturnLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierReturnId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    QuantityReturned = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BatchNumber = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_SupplierReturns_SupplierReturnId",
                        column: x => x.SupplierReturnId,
                        principalTable: "SupplierReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_WarehouseProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "WarehouseProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_WarehouseUnits_UnitId",
                        column: x => x.UnitId,
                        principalTable: "WarehouseUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    IngredientProductId = table.Column<int>(type: "int", nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualQuantityConsumed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AvailableStockAtExecution = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderLines_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrderLines_WarehouseProducts_IngredientProductId",
                        column: x => x.IngredientProductId,
                        principalTable: "WarehouseProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseItems_ExpenseId",
                table: "ExpenseItems",
                column: "ExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderLines_IngredientProductId",
                table: "ProductionOrderLines",
                column: "IngredientProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderLines_ProductionOrderId",
                table: "ProductionOrderLines",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_ProducedProductId",
                table: "ProductionOrders",
                column: "ProducedProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_ProductionRecipeId",
                table: "ProductionOrders",
                column: "ProductionRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_WarehouseId",
                table: "ProductionOrders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecipeIngredients_IngredientProductId",
                table: "ProductionRecipeIngredients",
                column: "IngredientProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecipeIngredients_ProductionRecipeId",
                table: "ProductionRecipeIngredients",
                column: "ProductionRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecipeIngredients_UnitId",
                table: "ProductionRecipeIngredients",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecipes_ProducedProductId",
                table: "ProductionRecipes",
                column: "ProducedProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecipes_YieldUnitId",
                table: "ProductionRecipes",
                column: "YieldUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_ProductId",
                table: "SupplierReturnLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_SupplierReturnId",
                table: "SupplierReturnLines",
                column: "SupplierReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_UnitId",
                table: "SupplierReturnLines",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_GoodsReceiptNoteId",
                table: "SupplierReturns",
                column: "GoodsReceiptNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_PurchaseOrderId",
                table: "SupplierReturns",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_SupplierId",
                table: "SupplierReturns",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_WarehouseId",
                table: "SupplierReturns",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchDailyPnLSnapshots");

            migrationBuilder.DropTable(
                name: "BranchInventoryCostBatches");

            migrationBuilder.DropTable(
                name: "ExpenseItems");

            migrationBuilder.DropTable(
                name: "ProductionOrderLines");

            migrationBuilder.DropTable(
                name: "ProductionRecipeIngredients");

            migrationBuilder.DropTable(
                name: "SupplierReturnLines");

            migrationBuilder.DropTable(
                name: "ProductionOrders");

            migrationBuilder.DropTable(
                name: "SupplierReturns");

            migrationBuilder.DropTable(
                name: "ProductionRecipes");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "IsReturn",
                table: "BranchRequisitions");
        }
    }
}
