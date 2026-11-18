using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseApiUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- 1. Warehouses ApiUrl
                IF COL_LENGTH('Warehouses', 'ApiUrl') IS NULL
                    ALTER TABLE Warehouses ADD ApiUrl nvarchar(max) NULL;

                -- 2. WarehouseProducts CategoryId
                IF COL_LENGTH('WarehouseProducts', 'CategoryId') IS NULL
                    ALTER TABLE WarehouseProducts ADD CategoryId int NULL;

                -- 3. SupplierPriceContracts IsPreferred
                IF COL_LENGTH('SupplierPriceContracts', 'IsPreferred') IS NULL
                    ALTER TABLE SupplierPriceContracts ADD IsPreferred bit NOT NULL DEFAULT 0;

                -- 4. BranchRequisitions RowVersion
                IF COL_LENGTH('BranchRequisitions', 'RowVersion') IS NULL
                    ALTER TABLE BranchRequisitions ADD RowVersion rowversion NULL;

                -- 5. WarehouseCategories Table
                IF OBJECT_ID('WarehouseCategories') IS NULL
                BEGIN
                    CREATE TABLE WarehouseCategories (
                        Id int NOT NULL PRIMARY KEY,
                        Code nvarchar(max) NOT NULL,
                        NameAr nvarchar(max) NOT NULL,
                        NameEn nvarchar(max) NULL,
                        Description nvarchar(max) NULL,
                        IsActive bit NOT NULL
                    );
                END

                -- 6. PurchaseOrderLogs Table
                IF OBJECT_ID('PurchaseOrderLogs') IS NULL
                BEGIN
                    CREATE TABLE PurchaseOrderLogs (
                        Id int NOT NULL IDENTITY(1, 1) PRIMARY KEY,
                        PurchaseOrderId int NOT NULL,
                        EventType nvarchar(max) NOT NULL,
                        OldStatus nvarchar(max) NULL,
                        NewStatus nvarchar(max) NULL,
                        Description nvarchar(max) NULL,
                        CreatedByUserId nvarchar(max) NULL,
                        CreatedByName nvarchar(max) NULL,
                        EventDate datetime2 NOT NULL,
                        CONSTRAINT FK_PurchaseOrderLogs_PurchaseOrders_PurchaseOrderId FOREIGN KEY (PurchaseOrderId) REFERENCES PurchaseOrders(Id) ON DELETE CASCADE
                    );
                END

                -- 7. RequisitionLogs Table
                IF OBJECT_ID('RequisitionLogs') IS NULL
                BEGIN
                    CREATE TABLE RequisitionLogs (
                        Id int NOT NULL IDENTITY(1, 1) PRIMARY KEY,
                        RequisitionId int NOT NULL,
                        EventType nvarchar(max) NOT NULL,
                        OldStatus nvarchar(max) NULL,
                        NewStatus nvarchar(max) NULL,
                        Description nvarchar(max) NULL,
                        ChangedByUserId nvarchar(max) NULL,
                        ChangedByName nvarchar(max) NULL,
                        ChangedByRole nvarchar(max) NULL,
                        EventDate datetime2 NOT NULL,
                        CONSTRAINT FK_RequisitionLogs_BranchRequisitions_RequisitionId FOREIGN KEY (RequisitionId) REFERENCES BranchRequisitions(Id) ON DELETE CASCADE
                    );
                END

                -- 8. Indexes & Foreign Keys
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WarehouseProducts_CategoryId')
                    CREATE INDEX IX_WarehouseProducts_CategoryId ON WarehouseProducts(CategoryId);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PurchaseOrderLogs_PurchaseOrderId')
                    CREATE INDEX IX_PurchaseOrderLogs_PurchaseOrderId ON PurchaseOrderLogs(PurchaseOrderId);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RequisitionLogs_RequisitionId')
                    CREATE INDEX IX_RequisitionLogs_RequisitionId ON RequisitionLogs(RequisitionId);

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WarehouseProducts_WarehouseCategories_CategoryId')
                    ALTER TABLE WarehouseProducts ADD CONSTRAINT FK_WarehouseProducts_WarehouseCategories_CategoryId FOREIGN KEY (CategoryId) REFERENCES WarehouseCategories(Id) ON DELETE NO ACTION;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseProducts_WarehouseCategories_CategoryId",
                table: "WarehouseProducts");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLogs");

            migrationBuilder.DropTable(
                name: "RequisitionLogs");

            migrationBuilder.DropTable(
                name: "WarehouseCategories");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseProducts_CategoryId",
                table: "WarehouseProducts");

            migrationBuilder.DropColumn(
                name: "ApiUrl",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "WarehouseProducts");

            migrationBuilder.DropColumn(
                name: "IsPreferred",
                table: "SupplierPriceContracts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BranchRequisitions");
        }
    }
}
