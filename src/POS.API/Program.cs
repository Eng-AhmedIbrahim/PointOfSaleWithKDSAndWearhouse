using POS.API.Hubs;
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5050", "http://localhost:5095");


builder.AddSerilogService();

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var encryptedConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseConnectionString = EncryptionHelper.DecryptString(encryptedConnectionString!);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddSwaggerServices();
builder.Services.AddApplicationServices();
builder.Services.AddSignalR();

var callCenterSettings = builder.Configuration.GetSection("CallCenterSettings")
    .Get<CallCenterSettings>() ?? new CallCenterSettings();
builder.Services.AddSingleton(callCenterSettings);

var isDispatcher = callCenterSettings.IsDispatcher 
    || (builder.Configuration.GetSection("DispatcherSettings")?.GetValue<bool>("IsDispatcher") ?? false);

// Only run if Central Call Center OR Dispatcher
if (callCenterSettings.IsCentralCallCenter || isDispatcher)
{
    builder.Services.AddHostedService<OrderRetryBackgroundService>();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", options =>
    {
        options.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true) // Allow any origin for SignalR
            .AllowCredentials(); // Required for SignalR
    });
});


#region Database connections


builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(databaseConnectionString);
});

builder.Services.AddDbContext<AppIdentityDbContext>(options =>
{
    options.UseSqlServer(databaseConnectionString);
});

#endregion

builder.Services.AddIdentityServices(builder.Configuration);

var app = builder.Build();

#region Database Migrate

using var scope = app.Services.CreateScope();

var services = scope.ServiceProvider;

var dbContext = services.GetRequiredService<AppDbContext>();
var identityDbContext = services.GetRequiredService<AppIdentityDbContext>();
var userManager = services.GetRequiredService<UserManager<AppUser>>();
var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

var loggerFactory = services.GetRequiredService<ILoggerFactory>();

try
{
    dbContext.Database.SetCommandTimeout(180);
    identityDbContext.Database.SetCommandTimeout(180);

    await dbContext.Database.MigrateAsync();
    await identityDbContext.Database.MigrateAsync();

    // Hotfix: Ensure Warehouse schema is always complete across all databases (PointOfSaleDB, PointOfSaleDBb, etc.)
    await dbContext.Database.ExecuteSqlRawAsync(@"
        IF COL_LENGTH('Warehouses', 'ApiUrl') IS NULL
            ALTER TABLE Warehouses ADD ApiUrl nvarchar(max) NULL;

        IF COL_LENGTH('WarehouseProducts', 'CategoryId') IS NULL
            ALTER TABLE WarehouseProducts ADD CategoryId int NULL;

        IF COL_LENGTH('SupplierPriceContracts', 'IsPreferred') IS NULL
            ALTER TABLE SupplierPriceContracts ADD IsPreferred bit NOT NULL DEFAULT 0;

        IF COL_LENGTH('BranchRequisitions', 'RowVersion') IS NULL
            ALTER TABLE BranchRequisitions ADD RowVersion rowversion NULL;

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
    ");

    // Hotfix: Ensure Permissions table schema is correct if migrations were out of sync
    await identityDbContext.Database.ExecuteSqlRawAsync(@"
        -- 1. Rename NameEn to ScreenEnglishName if it exists and target doesn't
        IF COL_LENGTH('Permissions', 'NameEn') IS NOT NULL AND COL_LENGTH('Permissions', 'ScreenEnglishName') IS NULL
            EXEC sp_rename 'Permissions.NameEn', 'ScreenEnglishName', 'COLUMN';

        -- 2. Rename NameAr to ScreenArabicName if it exists and target doesn't
        IF COL_LENGTH('Permissions', 'NameAr') IS NOT NULL AND COL_LENGTH('Permissions', 'ScreenArabicName') IS NULL
            EXEC sp_rename 'Permissions.NameAr', 'ScreenArabicName', 'COLUMN';

        -- 3. Add PoliceArabicName if missing
        IF COL_LENGTH('Permissions', 'PoliceArabicName') IS NULL
            ALTER TABLE Permissions ADD PoliceArabicName nvarchar(max) NOT NULL DEFAULT '';

        -- 4. Add PoliceEnglishNameEn if missing
        IF COL_LENGTH('Permissions', 'PoliceEnglishNameEn') IS NULL
            ALTER TABLE Permissions ADD PoliceEnglishNameEn nvarchar(max) NOT NULL DEFAULT '';

        -- 5. Add IsBackOffice if missing
        IF COL_LENGTH('Permissions', 'IsBackOffice') IS NULL
            ALTER TABLE Permissions ADD IsBackOffice bit NOT NULL DEFAULT 0;

        -- 6. Fallback: Add ScreenArabicName if still missing after rename
        IF COL_LENGTH('Permissions', 'ScreenArabicName') IS NULL
            ALTER TABLE Permissions ADD ScreenArabicName nvarchar(max) NOT NULL DEFAULT '';

        -- 7. Fallback: Add ScreenEnglishName if still missing after rename
        IF COL_LENGTH('Permissions', 'ScreenEnglishName') IS NULL
            ALTER TABLE Permissions ADD ScreenEnglishName nvarchar(max) NOT NULL DEFAULT '';
    ");

    await identityDbContext.Database.ExecuteSqlRawAsync(@"

    ");

    // Step 2: DML using the now-guaranteed columns - new batch so SQL Server recompiles with updated schema
    await identityDbContext.Database.ExecuteSqlRawAsync(@"
        -- Ensure new void & edit permissions are in Permissions table
        IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'CanAccessDeliveryVoidBtn')
            INSERT INTO Permissions (Name, PoliceArabicName, PoliceEnglishNameEn, ScreenArabicName, ScreenEnglishName, IsBackOffice)
            VALUES ('CanAccessDeliveryVoidBtn', N'إلغاء طلب دليفري', 'Void Delivery Order', N'شاشة التوصيل', 'Delivery Screen', 0);
        ELSE
            UPDATE Permissions SET PoliceArabicName = N'إلغاء طلب دليفري', ScreenArabicName = N'شاشة التوصيل' WHERE Name = 'CanAccessDeliveryVoidBtn';

        IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'CanAccessDeliveryEditBtn')
            INSERT INTO Permissions (Name, PoliceArabicName, PoliceEnglishNameEn, ScreenArabicName, ScreenEnglishName, IsBackOffice)
            VALUES ('CanAccessDeliveryEditBtn', N'تعديل طلب دليفري', 'Edit Delivery Order', N'شاشة التوصيل', 'Delivery Screen', 0);
        ELSE
            UPDATE Permissions SET PoliceArabicName = N'تعديل طلب دليفري', ScreenArabicName = N'شاشة التوصيل' WHERE Name = 'CanAccessDeliveryEditBtn';

        IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'CanVoidBranchDeliveryOrder')
            INSERT INTO Permissions (Name, PoliceArabicName, PoliceEnglishNameEn, ScreenArabicName, ScreenEnglishName, IsBackOffice)
            VALUES ('CanVoidBranchDeliveryOrder', N'إلغاء طلب الفروع', 'Void Branch Order', N'أوردرات الفروع', 'Branch Orders', 0);
        ELSE
            UPDATE Permissions SET PoliceArabicName = N'إلغاء طلب الفروع', ScreenArabicName = N'أوردرات الفروع' WHERE Name = 'CanVoidBranchDeliveryOrder';
    ");

    // Hotfix: Remove IDENTITY from Items, Categories, Attributes, and AttributeItems (Development Only - Tables are empty)
    await dbContext.Database.ExecuteSqlRawAsync(@"
        -- 0. Create ItemsClassifications without Identity if not exists
        IF OBJECT_ID('ItemsClassifications') IS NULL
        BEGIN
            CREATE TABLE ItemsClassifications (
                [Id] INT NOT NULL PRIMARY KEY,
                [Name] NVARCHAR(100) NOT NULL,
                [ArabicName] NVARCHAR(100) NULL
            );
        END

        IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id IN (OBJECT_ID('Categories'), OBJECT_ID('MenuSalesItems'), OBJECT_ID('Attributes'), OBJECT_ID('AttributeItems')))
        BEGIN
            -- 1. Drop all potentially conflicting FKs
            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuSalesItems_Categories_CategoryId')
                ALTER TABLE MenuSalesItems DROP CONSTRAINT FK_MenuSalesItems_Categories_CategoryId;
            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuSalesItems_ItemsClassifications_MainCategoryId')
                ALTER TABLE MenuSalesItems DROP CONSTRAINT FK_MenuSalesItems_ItemsClassifications_MainCategoryId;
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

            -- 1b. Fallback for existing tables without identity
            IF COL_LENGTH('MenuSalesItems', 'ByWeight') IS NULL AND OBJECT_ID('MenuSalesItems') IS NOT NULL
                ALTER TABLE MenuSalesItems ADD ByWeight BIT NOT NULL DEFAULT 0;

            IF COL_LENGTH('MenuSalesItems', 'MainCategoryId') IS NOT NULL AND OBJECT_ID('MenuSalesItems') IS NOT NULL
            BEGIN
                -- If it's nvarchar (old type), drop and recreate or alter
                DECLARE @dataType NVARCHAR(128) = (SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'MenuSalesItems' AND COLUMN_NAME = 'MainCategoryId');
                IF @dataType = 'nvarchar'
                BEGIN
                     -- Safest to just drop and let the identity check handle recreation, 
                     -- or if identity is already gone, manually alter.
                     -- But since we are in dev, let's just make sure it's INT.
                     ALTER TABLE MenuSalesItems ALTER COLUMN MainCategoryId INT NULL;
                END
            END

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

            -- 6. Restore FKs
            IF OBJECT_ID('Categories') IS NOT NULL AND OBJECT_ID('MenuSalesItems') IS NOT NULL
                ALTER TABLE MenuSalesItems ADD CONSTRAINT FK_MenuSalesItems_Categories_CategoryId FOREIGN KEY ([CategoryId]) REFERENCES Categories([Id]) ON DELETE SET NULL;
            IF OBJECT_ID('Attributes') IS NOT NULL AND OBJECT_ID('AttributeItems') IS NOT NULL
                ALTER TABLE AttributeItems ADD CONSTRAINT FK_AttributeItems_Attributes_AttributeId FOREIGN KEY ([AttributeId]) REFERENCES Attributes([Id]) ON DELETE CASCADE;
            IF OBJECT_ID('MenuSalesItems') IS NOT NULL AND OBJECT_ID('AttributeItems') IS NOT NULL
                ALTER TABLE AttributeItems ADD CONSTRAINT FK_AttributeItems_MenuSalesItems_RelatedMenuItemId FOREIGN KEY ([RelatedMenuItemId]) REFERENCES MenuSalesItems([Id]) ON DELETE CASCADE;
            IF OBJECT_ID('OrderItemAttributes') IS NOT NULL AND OBJECT_ID('AttributeItems') IS NOT NULL
                ALTER TABLE OrderItemAttributes ADD CONSTRAINT FK_OrderItemAttributes_AttributeItems_AttributeItemId FOREIGN KEY ([AttributeItemId]) REFERENCES AttributeItems([Id]) ON DELETE SET NULL;
            IF OBJECT_ID('MenuSalesItems') IS NOT NULL AND OBJECT_ID('ItemsClassifications') IS NOT NULL
                ALTER TABLE MenuSalesItems ADD CONSTRAINT FK_MenuSalesItems_ItemsClassifications_MainCategoryId FOREIGN KEY ([MainCategoryId]) REFERENCES ItemsClassifications([Id]) ON DELETE SET NULL;
        END
    ");

    // Hotfix: Ensure StaffMealGroups table schema is correct
    await dbContext.Database.ExecuteSqlRawAsync(@"
        IF OBJECT_ID('StaffMealGroups') IS NOT NULL
        BEGIN
            IF COL_LENGTH('StaffMealGroups', 'DailyLimit') IS NULL
                ALTER TABLE StaffMealGroups ADD DailyLimit INT NOT NULL DEFAULT 1;
            IF COL_LENGTH('StaffMealGroups', 'MealLimit') IS NULL
                ALTER TABLE StaffMealGroups ADD MealLimit INT NOT NULL DEFAULT 1;
            IF COL_LENGTH('StaffMealGroups', 'DailyAmountLimit') IS NULL
                ALTER TABLE StaffMealGroups ADD DailyAmountLimit DECIMAL(18,2) NOT NULL DEFAULT 0;
            -- Added safety for DailyLimit and MealLimit if missing from earlier migrations
        END

        IF OBJECT_ID('StaffMealConfigs') IS NOT NULL
        BEGIN
            IF COL_LENGTH('StaffMealConfigs', 'DailyLimit') IS NULL
                ALTER TABLE StaffMealConfigs ADD DailyLimit INT NOT NULL DEFAULT 1;
            IF COL_LENGTH('StaffMealConfigs', 'MealLimit') IS NULL
                ALTER TABLE StaffMealConfigs ADD MealLimit INT NOT NULL DEFAULT 1;
            IF COL_LENGTH('StaffMealConfigs', 'DailyAmountLimit') IS NULL
                ALTER TABLE StaffMealConfigs ADD DailyAmountLimit DECIMAL(18,2) NOT NULL DEFAULT 0;
        END
    ");


    await dbContext.Database.ExecuteSqlRawAsync(@"
        IF OBJECT_ID('ExpenseItems') IS NULL
        BEGIN
            CREATE TABLE ExpenseItems (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [ExpenseId] INT NOT NULL,
                [ItemType] INT NOT NULL DEFAULT 0,
                [ItemId] INT NULL,
                [ItemName] NVARCHAR(250) NOT NULL,
                [Quantity] DECIMAL(18,3) NOT NULL DEFAULT 1,
                [UnitPrice] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [TotalPrice] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [UnitName] NVARCHAR(50) NULL,
                [Notes] NVARCHAR(500) NULL,
                [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                CONSTRAINT [FK_ExpenseItems_Expenses_ExpenseId] FOREIGN KEY ([ExpenseId]) REFERENCES [Expenses] ([Id]) ON DELETE CASCADE
            );
        END

        IF OBJECT_ID('ProductionRecipes') IS NULL
        BEGIN
            CREATE TABLE ProductionRecipes (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [Code] NVARCHAR(100) NOT NULL,
                [Name] NVARCHAR(250) NOT NULL,
                [ProducedProductId] INT NOT NULL,
                [YieldQuantity] DECIMAL(18,3) NOT NULL DEFAULT 1,
                [YieldUnitId] INT NULL,
                [EstimatedBatchCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [EstimatedUnitCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [Notes] NVARCHAR(1000) NULL,
                [IsActive] BIT NOT NULL DEFAULT 1,
                [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                CONSTRAINT [FK_ProductionRecipes_WarehouseProducts] FOREIGN KEY ([ProducedProductId]) REFERENCES [WarehouseProducts] ([Id])
            );
        END

        IF OBJECT_ID('ProductionRecipeIngredients') IS NULL
        BEGIN
            CREATE TABLE ProductionRecipeIngredients (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [ProductionRecipeId] INT NOT NULL,
                [IngredientProductId] INT NOT NULL,
                [Quantity] DECIMAL(18,3) NOT NULL DEFAULT 0,
                [UnitId] INT NULL,
                [WastePercent] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [EstimatedCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [Notes] NVARCHAR(500) NULL,
                CONSTRAINT [FK_ProductionRecipeIngredients_ProductionRecipes] FOREIGN KEY ([ProductionRecipeId]) REFERENCES [ProductionRecipes] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_ProductionRecipeIngredients_WarehouseProducts] FOREIGN KEY ([IngredientProductId]) REFERENCES [WarehouseProducts] ([Id])
            );
        END

        IF OBJECT_ID('ProductionOrders') IS NULL
        BEGIN
            CREATE TABLE ProductionOrders (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [OrderNumber] NVARCHAR(100) NOT NULL,
                [WarehouseId] INT NOT NULL,
                [ProducedProductId] INT NOT NULL,
                [ProductionRecipeId] INT NULL,
                [TargetQuantity] DECIMAL(18,3) NOT NULL DEFAULT 1,
                [ActualQuantityProduced] DECIMAL(18,3) NOT NULL DEFAULT 1,
                [Status] INT NOT NULL DEFAULT 0,
                [OrderDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                [CompletedDate] DATETIME2 NULL,
                [TotalActualCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [UnitCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [Notes] NVARCHAR(1000) NULL,
                [CreatedByUserId] NVARCHAR(100) NULL,
                [CreatedByUserName] NVARCHAR(150) NULL,
                CONSTRAINT [FK_ProductionOrders_Warehouses] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]),
                CONSTRAINT [FK_ProductionOrders_WarehouseProducts] FOREIGN KEY ([ProducedProductId]) REFERENCES [WarehouseProducts] ([Id])
            );
        END

        IF OBJECT_ID('ProductionOrderLines') IS NULL
        BEGIN
            CREATE TABLE ProductionOrderLines (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [ProductionOrderId] INT NOT NULL,
                [IngredientProductId] INT NOT NULL,
                [PlannedQuantity] DECIMAL(18,3) NOT NULL DEFAULT 0,
                [ActualQuantityConsumed] DECIMAL(18,3) NOT NULL DEFAULT 0,
                [UnitCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [TotalCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [AvailableStockAtExecution] DECIMAL(18,3) NOT NULL DEFAULT 0,
                [Notes] NVARCHAR(500) NULL,
                CONSTRAINT [FK_ProductionOrderLines_ProductionOrders] FOREIGN KEY ([ProductionOrderId]) REFERENCES [ProductionOrders] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_ProductionOrderLines_WarehouseProducts] FOREIGN KEY ([IngredientProductId]) REFERENCES [WarehouseProducts] ([Id])
            );
        END

        IF COL_LENGTH('InventoryItems', 'UnitCost') IS NULL
            ALTER TABLE InventoryItems ADD UnitCost decimal(18,2) NOT NULL DEFAULT 0;

        IF COL_LENGTH('InventoryTransactions', 'UnitCost') IS NULL
            ALTER TABLE InventoryTransactions ADD UnitCost decimal(18,2) NOT NULL DEFAULT 0;

        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BranchInventoryCostBatches')
        BEGIN
            CREATE TABLE BranchInventoryCostBatches (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [BranchId] INT NOT NULL DEFAULT 1,
                [MenuSalesItemId] INT NOT NULL,
                [ItemName] NVARCHAR(250) NOT NULL,
                [BatchSource] NVARCHAR(50) NOT NULL,
                [ReferenceId] NVARCHAR(100) NOT NULL,
                [OriginalQuantity] DECIMAL(18,3) NOT NULL,
                [RemainingQuantity] DECIMAL(18,3) NOT NULL,
                [UnitCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [UnitName] NVARCHAR(50) NULL,
                [Priority] INT NOT NULL DEFAULT 2,
                [IsExhausted] BIT NOT NULL DEFAULT 0,
                [ReceivedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );
        END

        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BranchDailyPnLSnapshots')
        BEGIN
            CREATE TABLE BranchDailyPnLSnapshots (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [Date] DATETIME2 NOT NULL,
                [BranchId] INT NOT NULL DEFAULT 1,
                [BranchName] NVARCHAR(200) NULL,
                [TotalGrossSales] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [TotalDiscounts] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [TotalVoids] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [NetSales] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [TotalCOGS] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [TotalWasteCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [TotalInventoryPurchases] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [GrossProfit] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [GrossProfitMargin] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [TotalExpenses] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [NetProfit] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [OrderCount] INT NOT NULL DEFAULT 0,
                [CalculatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );
        END
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BranchDailyPnLSnapshots') AND name = 'TotalVoids')
                ALTER TABLE BranchDailyPnLSnapshots ADD [TotalVoids] DECIMAL(18,2) NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BranchDailyPnLSnapshots') AND name = 'TotalWasteCost')
                ALTER TABLE BranchDailyPnLSnapshots ADD [TotalWasteCost] DECIMAL(18,2) NOT NULL DEFAULT 0;
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BranchDailyPnLSnapshots') AND name = 'TotalInventoryPurchases')
                ALTER TABLE BranchDailyPnLSnapshots ADD [TotalInventoryPurchases] DECIMAL(18,2) NOT NULL DEFAULT 0;
        END

        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BranchRequisitions') AND name = 'IsReturn')
        BEGIN
            ALTER TABLE BranchRequisitions ADD [IsReturn] BIT NOT NULL DEFAULT 0;
        END

        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SupplierReturns')
        BEGIN
            CREATE TABLE SupplierReturns (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [ReturnNumber] NVARCHAR(50) NOT NULL,
                [PurchaseOrderId] INT NULL,
                [GoodsReceiptNoteId] INT NULL,
                [WarehouseId] INT NOT NULL,
                [SupplierId] INT NOT NULL,
                [ReturnDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                [Reason] NVARCHAR(500) NULL,
                [ReturnedByUserId] NVARCHAR(450) NULL,
                [TotalAmount] DECIMAL(18,2) NOT NULL DEFAULT 0
            );
        END

        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SupplierReturnLines')
        BEGIN
            CREATE TABLE SupplierReturnLines (
                [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [SupplierReturnId] INT NOT NULL,
                [ProductId] INT NOT NULL,
                [UnitId] INT NOT NULL,
                [QuantityReturned] DECIMAL(18,4) NOT NULL DEFAULT 0,
                [UnitCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [TotalCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
                [Reason] NVARCHAR(500) NULL,
                [BatchNumber] NVARCHAR(100) NULL
            );
        END
    ");

    await PosDbContextDataSeed.SeedAsync(dbContext);
    await AppIdentityDbContextSeed.SeedAsync(userManager, roleManager, identityDbContext);
}
catch (Exception ex)
{
    var logger = loggerFactory.CreateLogger<Program>();
    logger.LogError(ex, "An error occurred during migration");
}

#endregion

#region Configure Kestrel Middelewares

app.UseMiddleware<ExeptionMiddleWare>();

app.UseSwaggerServices();

app.UseStatusCodePagesWithReExecute("/errors/{0}");

// app.UseHttpsRedirection();

app.UseCors("MyPolicy");
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { status = "online", service = "POS & Warehouse API", timestamp = DateTime.UtcNow })).AllowAnonymous();
app.MapGet("/ping", () => Results.Ok(new { status = "online", timestamp = DateTime.UtcNow })).AllowAnonymous();

app.MapControllers();
app.MapHub<DeliveryHub>("/callcenterhub");
app.MapHub<WarehouseHub>("/hubs/warehouse");
#endregion

await app.RunAsync();
