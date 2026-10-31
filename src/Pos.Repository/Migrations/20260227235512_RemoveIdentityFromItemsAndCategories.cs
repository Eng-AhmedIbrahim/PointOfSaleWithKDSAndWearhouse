using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIdentityFromItemsAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Backup/Drop/Recreate logic
                IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id IN (OBJECT_ID('Categories'), OBJECT_ID('MenuSalesItems'), OBJECT_ID('Attributes'), OBJECT_ID('AttributeItems')))
                BEGIN
                    -- 1. Drop all potentially conflicting FKs
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuSalesItems_Categories_CategoryId')
                        ALTER TABLE MenuSalesItems DROP CONSTRAINT FK_MenuSalesItems_Categories_CategoryId;
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttributeItems_Attributes_AttributeId')
                        ALTER TABLE AttributeItems DROP CONSTRAINT FK_AttributeItems_Attributes_AttributeId;
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttributeItems_MenuSalesItems_RelatedMenuItemId')
                        ALTER TABLE AttributeItems DROP CONSTRAINT FK_AttributeItems_MenuSalesItems_RelatedMenuItemId;
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrdersDetails_MenuSalesItems_ItemId')
                        ALTER TABLE OrdersDetails DROP CONSTRAINT FK_OrdersDetails_MenuSalesItems_ItemId;
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrdersDetails_MenuSalesItems_MenuSalesItemId')
                        ALTER TABLE OrdersDetails DROP CONSTRAINT FK_OrdersDetails_MenuSalesItems_MenuSalesItemId;
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuSalesItems_Attributes_AttributeId')
                        ALTER TABLE MenuSalesItems DROP CONSTRAINT FK_MenuSalesItems_Attributes_AttributeId;
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItemAttributes_MenuSalesItems_MenuSalesItemId')
                        ALTER TABLE OrderItemAttributes DROP CONSTRAINT FK_OrderItemAttributes_MenuSalesItems_MenuSalesItemId;
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItemAttributes_MenuSalesItems_ItemId')
                        ALTER TABLE OrderItemAttributes DROP CONSTRAINT FK_OrderItemAttributes_MenuSalesItems_ItemId;
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItemAttributes_AttributeItems_AttributeItemId')
                        ALTER TABLE OrderItemAttributes DROP CONSTRAINT FK_OrderItemAttributes_AttributeItems_AttributeItemId;

                    -- 2. Drop and Recreate Categories without Identity
                    IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('Categories'))
                    BEGIN
                        DROP TABLE Categories;
                        CREATE TABLE Categories ( 
                            [Id] INT NOT NULL PRIMARY KEY, 
                            [ArabicName] NVARCHAR(70) NOT NULL, 
                            [EnglishName] NVARCHAR(70) NOT NULL, 
                            [NormalizedEnglishName] NVARCHAR(70) NOT NULL, 
                            [Invisible] BIT NOT NULL DEFAULT 0, 
                            [ItemsFont] NVARCHAR(70) NULL, 
                            [UpdateDate] DATETIME NULL, 
                            [PrintInBackupReceipt] BIT NOT NULL DEFAULT 1, 
                            [CreationDate] DATETIME NOT NULL, 
                            [BranchId] INT NOT NULL DEFAULT 1, 
                            [KitchenTypeId] INT NULL 
                        );
                    END

                    -- 3. Drop and Recreate Attributes without Identity
                    IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('Attributes'))
                    BEGIN
                        DROP TABLE Attributes;
                        CREATE TABLE Attributes ( 
                            [Id] INT NOT NULL PRIMARY KEY, 
                            [ArabicName] NVARCHAR(255) NOT NULL, 
                            [EnglishName] NVARCHAR(255) NOT NULL 
                        );
                    END

                    -- 4. Drop and Recreate MenuSalesItems without Identity
                    IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('MenuSalesItems'))
                    BEGIN
                        DROP TABLE MenuSalesItems;
                        CREATE TABLE MenuSalesItems ( 
                            [Id] INT NOT NULL PRIMARY KEY, 
                            [ArabicName] NVARCHAR(70) NOT NULL, 
                            [EnglishName] NVARCHAR(70) NOT NULL, 
                            [NormalizedEnglishName] NVARCHAR(70) NOT NULL, 
                            [Price] DECIMAL(18,2) NULL, 
                            [CategoryId] INT NULL, 
                            [Barcode] NVARCHAR(MAX) NULL, 
                            [Invisible] BIT NOT NULL DEFAULT 0, 
                            [CreationDate] DATETIME2 NOT NULL, 
                            [UpdatedDate] DATETIME2 NULL, 
                            [BranchId] INT NULL, 
                            [AttributeId] INT NULL, 
                            [AttributePrice] DECIMAL(18,2) NULL, 
                            [HasAttribute] BIT NOT NULL DEFAULT 0, 
                            [Description] NVARCHAR(255) NULL, 
                            [ImagePath] NVARCHAR(255) NULL, 
                            [BackColor] NVARCHAR(7) NULL, 
                            [TextColor] NVARCHAR(7) NULL, 
                            [TextSize] INT NULL, 
                            [FirstPrice] DECIMAL(18,2) NULL, 
                            [SecondPrice] DECIMAL(18,2) NULL, 
                            [ThirdPrice] DECIMAL(18,2) NULL, 
                            [FourthPrice] DECIMAL(18,2) NULL, 
                            [FifthPrice] DECIMAL(18,2) NULL, 
                            [Tax] DECIMAL(18,2) NULL, 
                            [PrintInBackupReceipt] BIT NOT NULL DEFAULT 1, 
                            [KitchenTypeId] INT NULL, 
                            [MainCategoryId] INT NULL, 
                            [ByWeight] BIT NOT NULL DEFAULT 0 
                        );
                    END

                    -- 5. Drop and Recreate AttributeItems without Identity
                    IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('AttributeItems'))
                    BEGIN
                        DROP TABLE AttributeItems;
                        CREATE TABLE AttributeItems ( 
                            [Id] INT NOT NULL PRIMARY KEY, 
                            [AppearanceIndex] INT NOT NULL, 
                            [AttributeId] INT NOT NULL, 
                            [RelatedMenuItemId] INT NOT NULL 
                        );
                    END

                    -- Restore FKs with NO ACTION to avoid cycles during migration
                    IF OBJECT_ID('Categories') IS NOT NULL AND OBJECT_ID('MenuSalesItems') IS NOT NULL
                        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuSalesItems_Categories_CategoryId')
                            ALTER TABLE MenuSalesItems ADD CONSTRAINT FK_MenuSalesItems_Categories_CategoryId FOREIGN KEY ([CategoryId]) REFERENCES Categories([Id]) ON DELETE NO ACTION;

                    IF OBJECT_ID('Attributes') IS NOT NULL AND OBJECT_ID('AttributeItems') IS NOT NULL
                        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttributeItems_Attributes_AttributeId')
                            ALTER TABLE AttributeItems ADD CONSTRAINT FK_AttributeItems_Attributes_AttributeId FOREIGN KEY ([AttributeId]) REFERENCES Attributes([Id]) ON DELETE NO ACTION;

                    IF OBJECT_ID('MenuSalesItems') IS NOT NULL AND OBJECT_ID('AttributeItems') IS NOT NULL
                        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AttributeItems_MenuSalesItems_RelatedMenuItemId')
                            ALTER TABLE AttributeItems ADD CONSTRAINT FK_AttributeItems_MenuSalesItems_RelatedMenuItemId FOREIGN KEY ([RelatedMenuItemId]) REFERENCES MenuSalesItems([Id]) ON DELETE NO ACTION;

                    IF OBJECT_ID('OrderItemAttributes') IS NOT NULL AND OBJECT_ID('AttributeItems') IS NOT NULL
                        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItemAttributes_AttributeItems_AttributeItemId')
                            ALTER TABLE OrderItemAttributes ADD CONSTRAINT FK_OrderItemAttributes_AttributeItems_AttributeItemId FOREIGN KEY ([AttributeItemId]) REFERENCES AttributeItems([Id]) ON DELETE NO ACTION;

                    IF OBJECT_ID('MenuSalesItems') IS NOT NULL AND OBJECT_ID('Attributes') IS NOT NULL
                        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuSalesItems_Attributes_AttributeId')
                            ALTER TABLE MenuSalesItems ADD CONSTRAINT FK_MenuSalesItems_Attributes_AttributeId FOREIGN KEY ([AttributeId]) REFERENCES Attributes([Id]) ON DELETE NO ACTION;

                    IF OBJECT_ID('OrdersDetails') IS NOT NULL AND OBJECT_ID('MenuSalesItems') IS NOT NULL
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrdersDetails_MenuSalesItems_MenuSalesItemId')
                            ALTER TABLE OrdersDetails ADD CONSTRAINT FK_OrdersDetails_MenuSalesItems_MenuSalesItemId FOREIGN KEY ([MenuSalesItemId]) REFERENCES MenuSalesItems([Id]) ON DELETE NO ACTION;
                    END

                    IF OBJECT_ID('Branches') IS NOT NULL
                    BEGIN
                        IF OBJECT_ID('Categories') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Categories_Branches_BranchId')
                            ALTER TABLE Categories ADD CONSTRAINT FK_Categories_Branches_BranchId FOREIGN KEY ([BranchId]) REFERENCES Branches([Id]) ON DELETE NO ACTION;
                        IF OBJECT_ID('MenuSalesItems') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuSalesItems_Branches_BranchId')
                            ALTER TABLE MenuSalesItems ADD CONSTRAINT FK_MenuSalesItems_Branches_BranchId FOREIGN KEY ([BranchId]) REFERENCES Branches([Id]) ON DELETE NO ACTION;
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
