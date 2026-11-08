namespace POS.Services.DataSyncServices;

public class DataSyncService : IDataSyncService
{
    private readonly AppDbContext _context;
    private readonly IEncryptionService _encryption;

    public DataSyncService(AppDbContext context, IEncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    public async Task<HqSettingDto> GetHqSettingsAsync()
    {
        var setting = await _context.HqSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (setting == null) return new HqSettingDto();

        return new HqSettingDto
        {
            Id = setting.Id,
            ServerName = setting.ServerName,
            DatabaseName = _encryption.Decrypt(setting.DatabaseName),
            UserName = _encryption.Decrypt(setting.UserName),
            Password = _encryption.Decrypt(setting.Password),
            UseIntegratedSecurity = setting.UseIntegratedSecurity,
            IsEnabled = setting.IsEnabled,
            LastSyncDate = setting.LastSyncDate,
            ConnectTimeout = setting.ConnectTimeout,
            TrustServerCertificate = setting.TrustServerCertificate,
            Encrypt = setting.Encrypt
        };
    }

    public async Task<BaseResponse> UpdateHqSettingsAsync(HqSettingDto settings)
    {
        var existing = await _context.HqSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (existing == null)
        {
            existing = new HqSetting
            {
                ServerName = settings.ServerName,
                DatabaseName = _encryption.Encrypt(settings.DatabaseName),
                UserName = _encryption.Encrypt(settings.UserName),
                Password = _encryption.Encrypt(settings.Password),
                UseIntegratedSecurity = settings.UseIntegratedSecurity,
                IsEnabled = settings.IsEnabled,
                ConnectTimeout = settings.ConnectTimeout,
                TrustServerCertificate = settings.TrustServerCertificate,
                Encrypt = settings.Encrypt
            };
            _context.HqSettings.Add(existing);
        }
        else
        {
            existing.ServerName = settings.ServerName;
            existing.DatabaseName = _encryption.Encrypt(settings.DatabaseName);
            existing.UserName = _encryption.Encrypt(settings.UserName);
            existing.Password = _encryption.Encrypt(settings.Password);
            existing.UseIntegratedSecurity = settings.UseIntegratedSecurity;
            existing.IsEnabled = settings.IsEnabled;
            existing.ConnectTimeout = settings.ConnectTimeout;
            existing.TrustServerCertificate = settings.TrustServerCertificate;
            existing.Encrypt = settings.Encrypt;
        }

        await _context.SaveChangesAsync();
        return new BaseResponse { Success = true, Message = "Settings updated successfully" };
    }

    public async Task<BaseResponse> TestHqConnectionAsync(HqSettingDto settings)
    {
        string connString = BuildConnectionString(settings);

        try
        {
            using (var conn = new SqlConnection(connString))
            {
                await conn.OpenAsync();
                return new BaseResponse { Success = true, Message = "Connection successful!" };
            }
        }
        catch (Exception ex)
        {
            return new BaseResponse { Success = false, Message = $"Connection failed: {ex.Message}" };
        }
    }

    public async Task<BaseResponse> SyncDataFromHqAsync(SyncRequestDto request)
    {
        var setting = await _context.HqSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (setting == null)
            return new BaseResponse { Success = false, Message = "HQ Settings not configured. Please save the connection settings first." };

        var appDate = await _context.AppDate.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        int localBranchId = appDate?.BranchId ?? 0;

        var decryptedSetting = new HqSetting
        {
            ServerName = setting.ServerName,
            DatabaseName = _encryption.Decrypt(setting.DatabaseName),
            UserName = _encryption.Decrypt(setting.UserName),
            Password = _encryption.Decrypt(setting.Password),
            UseIntegratedSecurity = setting.UseIntegratedSecurity,
            ConnectTimeout = setting.ConnectTimeout,
            TrustServerCertificate = setting.TrustServerCertificate,
            Encrypt = setting.Encrypt
        };

        var hqConnString = decryptedSetting.GetConnectionString();
        var localConnString = _context.Database.GetConnectionString() ?? throw new InvalidOperationException("Local connection string not found.");

        using (var localConn = new SqlConnection(localConnString))
        {
            await localConn.OpenAsync();
            using (var transaction = localConn.BeginTransaction())
            {
                try
                {
                    var disableAll = new SqlCommand("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'", localConn, transaction);
                    await disableAll.ExecuteNonQueryAsync();

                    var tablesToSync = new List<string>();

                    if (request.SyncMasterData)
                    {
                        tablesToSync.AddRange(new[] { "Companies", "Branches" });
                    }

                    // Perform Sync for Master Data
                    foreach (var table in tablesToSync)
                    {
                        await SyncTable(hqConnString, table, localConn, transaction, localBranchId);
                    }

                    // Incremental Sales Items Sync: Preserves local kitchen printers, local items, and matches IDs exactly
                    if (request.SyncSalesItems)
                    {
                        await SyncSalesItemsIncremental(hqConnString, localConn, transaction, localBranchId);
                    }

                    // Warehouse Supply & Raw Materials Incremental Sync (Preserves Branch Quantities!)
                    if (request.SyncStoreItems || request.SyncWarehouses)
                    {
                        await SyncWarehouseUnits(hqConnString, localConn, transaction);
                        await SyncWarehouseCategories(hqConnString, localConn, transaction);
                        await SyncWarehouseProductsIncremental(hqConnString, localConn, transaction, localBranchId);
                    }

                    // 2. Re-enable all constraints
                    var enableAll = new SqlCommand("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'", localConn, transaction);
                    await enableAll.ExecuteNonQueryAsync();

                    transaction.Commit();

                    setting.LastSyncDate = DateTime.Now;
                    await _context.SaveChangesAsync();

                    return new BaseResponse { Success = true, Message = "Synchronization completed successfully. Data is now identical to HQ while keeping local settings." };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new BaseResponse { Success = false, Message = $"Synchronization failed: {ex.Message}" };
                }
            }
        }
    }

    public async Task<List<string>> GetDatabasesAsync(HqSettingDto settings)
    {
        var databases = new List<string>();
        var masterSettings = new HqSettingDto
        {
            ServerName = settings.ServerName,
            DatabaseName = "master",
            UserName = settings.UserName,
            Password = settings.Password,
            UseIntegratedSecurity = settings.UseIntegratedSecurity,
            ConnectTimeout = 5,
            TrustServerCertificate = settings.TrustServerCertificate,
            Encrypt = settings.Encrypt
        };

        string connString = BuildConnectionString(masterSettings);

        try
        {
            using (var conn = new SqlConnection(connString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT name FROM sys.databases WHERE database_id > 4", conn);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        databases.Add(reader.GetString(0));
                    }
                }
            }
        }
        catch { }
        return databases;
    }

    private async Task SyncTable(string hqConnString, string tableName, SqlConnection localConn, SqlTransaction transaction, int localBranchId)
    {
        using (var hqConn = new SqlConnection(hqConnString))
        {
            await hqConn.OpenAsync();
            var cmd = new SqlCommand($"SELECT * FROM {tableName}", hqConn);
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                var dt = new DataTable();
                dt.Load(reader);

                var branchIdColumn = dt.Columns.Cast<DataColumn>()
                    .FirstOrDefault(c => string.Equals(c.ColumnName, "BranchId", StringComparison.OrdinalIgnoreCase) || 
                                         string.Equals(c.ColumnName, "BranchID", StringComparison.OrdinalIgnoreCase));

                if (branchIdColumn != null && localBranchId > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        row[branchIdColumn] = localBranchId;
                    }
                }

                var disableCmd = new SqlCommand($"ALTER TABLE {tableName} NOCHECK CONSTRAINT ALL", localConn, transaction);
                await disableCmd.ExecuteNonQueryAsync();

                var deleteCmd = new SqlCommand($"DELETE FROM {tableName}", localConn, transaction);
                await deleteCmd.ExecuteNonQueryAsync();

                if (dt.Rows.Count > 0)
                {
                    using (var bulkCopy = new SqlBulkCopy(localConn, SqlBulkCopyOptions.KeepIdentity, transaction))
                    {
                        bulkCopy.DestinationTableName = tableName;
                        foreach (DataColumn column in dt.Columns)
                        {
                            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                        }
                        await bulkCopy.WriteToServerAsync(dt);
                    }
                }

                var enableCmd = new SqlCommand($"ALTER TABLE {tableName} CHECK CONSTRAINT ALL", localConn, transaction);
                await enableCmd.ExecuteNonQueryAsync();
            }
        }
    }

    private string BuildConnectionString(HqSettingDto settings)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = settings.ServerName,
            InitialCatalog = settings.DatabaseName,
            IntegratedSecurity = settings.UseIntegratedSecurity,
            ConnectTimeout = settings.ConnectTimeout,
            TrustServerCertificate = settings.TrustServerCertificate,
            Encrypt = settings.Encrypt
        };

        if (!settings.UseIntegratedSecurity)
        {
            builder.UserID = settings.UserName;
            builder.Password = settings.Password;
        }

        return builder.ConnectionString;
    }

    private async Task SyncWarehouseUnits(string hqConnString, SqlConnection localConn, SqlTransaction transaction)
    {
        using (var hqConn = new SqlConnection(hqConnString))
        {
            await hqConn.OpenAsync();
            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WarehouseUnits'", hqConn);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) == 0) return;

            var cmd = new SqlCommand("SELECT Id, NameAr, NameEn, Symbol, IsBaseUnit FROM WarehouseUnits", hqConn);
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                var dt = new DataTable();
                dt.Load(reader);

                foreach (DataRow row in dt.Rows)
                {
                    int id = Convert.ToInt32(row["Id"]);
                    string nameAr = row["NameAr"]?.ToString() ?? "";
                    string nameEn = row["NameEn"]?.ToString() ?? "";
                    string symbol = row["Symbol"]?.ToString() ?? "";
                    bool isBaseUnit = Convert.ToBoolean(row["IsBaseUnit"]);

                    var mergeSql = @"
IF EXISTS (SELECT 1 FROM WarehouseUnits WHERE Id = @Id)
BEGIN
    UPDATE WarehouseUnits
    SET NameAr = @NameAr, NameEn = @NameEn, Symbol = @Symbol, IsBaseUnit = @IsBaseUnit
    WHERE Id = @Id;
END
ELSE
BEGIN
    SET IDENTITY_INSERT WarehouseUnits ON;
    INSERT INTO WarehouseUnits (Id, NameAr, NameEn, Symbol, IsBaseUnit)
    VALUES (@Id, @NameAr, @NameEn, @Symbol, @IsBaseUnit);
    SET IDENTITY_INSERT WarehouseUnits OFF;
END";
                    using (var upsertCmd = new SqlCommand(mergeSql, localConn, transaction))
                    {
                        upsertCmd.Parameters.AddWithValue("@Id", id);
                        upsertCmd.Parameters.AddWithValue("@NameAr", nameAr);
                        upsertCmd.Parameters.AddWithValue("@NameEn", (object?)nameEn ?? DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@Symbol", (object?)symbol ?? DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@IsBaseUnit", isBaseUnit);
                        await upsertCmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }
    }

    private async Task SyncWarehouseCategories(string hqConnString, SqlConnection localConn, SqlTransaction transaction)
    {
        using (var hqConn = new SqlConnection(hqConnString))
        {
            await hqConn.OpenAsync();
            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WarehouseCategories'", hqConn);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) == 0) return;

            var cmd = new SqlCommand("SELECT Id, Code, NameAr, NameEn, Description, IsActive FROM WarehouseCategories", hqConn);
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                var dt = new DataTable();
                dt.Load(reader);

                foreach (DataRow row in dt.Rows)
                {
                    int id = Convert.ToInt32(row["Id"]);
                    string code = row["Code"]?.ToString() ?? "";
                    string nameAr = row["NameAr"]?.ToString() ?? "";
                    string nameEn = row["NameEn"]?.ToString() ?? "";
                    string desc = row["Description"]?.ToString() ?? "";
                    bool isActive = Convert.ToBoolean(row["IsActive"]);

                    var mergeSql = @"
IF EXISTS (SELECT 1 FROM WarehouseCategories WHERE Id = @Id)
BEGIN
    UPDATE WarehouseCategories
    SET Code = @Code, NameAr = @NameAr, NameEn = @NameEn, Description = @Description, IsActive = @IsActive
    WHERE Id = @Id;
END
ELSE
BEGIN
    INSERT INTO WarehouseCategories (Id, Code, NameAr, NameEn, Description, IsActive)
    VALUES (@Id, @Code, @NameAr, @NameEn, @Description, @IsActive);
END";
                    using (var upsertCmd = new SqlCommand(mergeSql, localConn, transaction))
                    {
                        upsertCmd.Parameters.AddWithValue("@Id", id);
                        upsertCmd.Parameters.AddWithValue("@Code", code);
                        upsertCmd.Parameters.AddWithValue("@NameAr", nameAr);
                        upsertCmd.Parameters.AddWithValue("@NameEn", (object?)nameEn ?? DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@Description", (object?)desc ?? DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@IsActive", isActive);
                        await upsertCmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }
    }

    private async Task SyncWarehouseProductsIncremental(string hqConnString, SqlConnection localConn, SqlTransaction transaction, int localBranchId)
    {
        using (var hqConn = new SqlConnection(hqConnString))
        {
            await hqConn.OpenAsync();
            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WarehouseProducts'", hqConn);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) == 0) return;

            var cmd = new SqlCommand(@"
SELECT Id, SKU, Barcode, NameAr, NameEn, Description, CategoryId, CategoryName,
       BaseUnitId, WeightedAverageCost, LastPurchaseCost, MinStockLevel,
       ReorderPoint, ReorderQuantity, IsActive, CreatedAt
FROM WarehouseProducts", hqConn);

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                var dt = new DataTable();
                dt.Load(reader);

                foreach (DataRow row in dt.Rows)
                {
                    int hqId = Convert.ToInt32(row["Id"]);
                    string sku = row["SKU"]?.ToString() ?? "";
                    string barcode = row["Barcode"]?.ToString() ?? "";
                    string nameAr = row["NameAr"]?.ToString() ?? "";
                    string nameEn = row["NameEn"]?.ToString() ?? "";
                    string desc = row["Description"]?.ToString() ?? "";
                    object categoryId = row["CategoryId"] != DBNull.Value ? row["CategoryId"] : DBNull.Value;
                    string categoryName = row["CategoryName"]?.ToString() ?? "";
                    int baseUnitId = Convert.ToInt32(row["BaseUnitId"]);
                    decimal weightedAvgCost = Convert.ToDecimal(row["WeightedAverageCost"]);
                    decimal lastPurchaseCost = Convert.ToDecimal(row["LastPurchaseCost"]);
                    decimal minStock = Convert.ToDecimal(row["MinStockLevel"]);
                    decimal reorderPoint = Convert.ToDecimal(row["ReorderPoint"]);
                    decimal reorderQty = Convert.ToDecimal(row["ReorderQuantity"]);
                    bool isActive = Convert.ToBoolean(row["IsActive"]);
                    DateTime createdAt = Convert.ToDateTime(row["CreatedAt"]);

                    // Incremental Upsert matching on SKU or Id:
                    // Crucial: Updates ONLY definitions/metadata, leaving all stock/quantities untouched!
                    var upsertSql = @"
IF EXISTS (SELECT 1 FROM WarehouseProducts WHERE SKU = @SKU OR Id = @Id)
BEGIN
    UPDATE WarehouseProducts
    SET Barcode = @Barcode,
        NameAr = @NameAr,
        NameEn = @NameEn,
        Description = @Description,
        CategoryId = @CategoryId,
        CategoryName = @CategoryName,
        BaseUnitId = @BaseUnitId,
        LastPurchaseCost = @LastPurchaseCost,
        MinStockLevel = @MinStockLevel,
        ReorderPoint = @ReorderPoint,
        ReorderQuantity = @ReorderQuantity,
        IsActive = @IsActive
    WHERE SKU = @SKU OR Id = @Id;
END
ELSE
BEGIN
    SET IDENTITY_INSERT WarehouseProducts ON;
    INSERT INTO WarehouseProducts (Id, SKU, Barcode, NameAr, NameEn, Description, CategoryId, CategoryName,
                                   BaseUnitId, WeightedAverageCost, LastPurchaseCost, MinStockLevel,
                                   ReorderPoint, ReorderQuantity, IsActive, CreatedAt)
    VALUES (@Id, @SKU, @Barcode, @NameAr, @NameEn, @Description, @CategoryId, @CategoryName,
            @BaseUnitId, @WeightedAverageCost, @LastPurchaseCost, @MinStockLevel,
            @ReorderPoint, @ReorderQuantity, @IsActive, @CreatedAt);
    SET IDENTITY_INSERT WarehouseProducts OFF;
END";

                    using (var upsertCmd = new SqlCommand(upsertSql, localConn, transaction))
                    {
                        upsertCmd.Parameters.AddWithValue("@Id", hqId);
                        upsertCmd.Parameters.AddWithValue("@SKU", sku);
                        upsertCmd.Parameters.AddWithValue("@Barcode", (object?)barcode ?? DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@NameAr", nameAr);
                        upsertCmd.Parameters.AddWithValue("@NameEn", (object?)nameEn ?? DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@Description", (object?)desc ?? DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@CategoryId", categoryId);
                        upsertCmd.Parameters.AddWithValue("@CategoryName", (object?)categoryName ?? DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@BaseUnitId", baseUnitId);
                        upsertCmd.Parameters.AddWithValue("@WeightedAverageCost", weightedAvgCost);
                        upsertCmd.Parameters.AddWithValue("@LastPurchaseCost", lastPurchaseCost);
                        upsertCmd.Parameters.AddWithValue("@MinStockLevel", minStock);
                        upsertCmd.Parameters.AddWithValue("@ReorderPoint", reorderPoint);
                        upsertCmd.Parameters.AddWithValue("@ReorderQuantity", reorderQty);
                        upsertCmd.Parameters.AddWithValue("@IsActive", isActive);
                        upsertCmd.Parameters.AddWithValue("@CreatedAt", createdAt);
                        await upsertCmd.ExecuteNonQueryAsync();
                    }

                    // Seamless Bridge: Auto-link to MenuSalesItems as Raw Material & ensure InventoryItem exists for Recipe
                    var bridgeSql = @"
DECLARE @targetMenuId INT = NULL;
DECLARE @effBranchId INT = @BranchId;

IF @effBranchId <= 0
BEGIN
    SELECT TOP 1 @effBranchId = Id FROM Branches WHERE IsMain = 1;
    IF @effBranchId IS NULL SELECT TOP 1 @effBranchId = Id FROM Branches;
END

-- 1. Try match existing item by Barcode (if not empty)
IF @Barcode IS NOT NULL AND LTRIM(RTRIM(@Barcode)) <> ''
BEGIN
    SELECT TOP 1 @targetMenuId = Id FROM MenuSalesItems WHERE Barcode = @Barcode;
END

-- 2. Try match existing item by Arabic Name
IF @targetMenuId IS NULL AND @NameAr IS NOT NULL AND LTRIM(RTRIM(@NameAr)) <> ''
BEGIN
    SELECT TOP 1 @targetMenuId = Id FROM MenuSalesItems WHERE ArabicName = @NameAr;
END

-- 3. If found: ensure Barcode is updated if empty, and marked as IsInventory
IF @targetMenuId IS NOT NULL
BEGIN
    UPDATE MenuSalesItems
    SET Barcode = CASE WHEN (Barcode IS NULL OR Barcode = '') AND @Barcode IS NOT NULL AND @Barcode <> '' THEN @Barcode ELSE Barcode END,
        IsInventory = 1
    WHERE Id = @targetMenuId;
END
ELSE
BEGIN
    -- 4. Not found: Insert new raw material safely in MenuSalesItems (will never overwrite existing items)
    INSERT INTO MenuSalesItems (
        ArabicName, EnglishName, NormalizedEnglishName, Barcode,
        Price, Invisible, ItemTypeId, IsInventory, CreationDate, HasAttribute, ByWeight
    ) VALUES (
        @NameAr,
        ISNULL(NULLIF(@NameEn, ''), @NameAr),
        UPPER(ISNULL(NULLIF(@NameEn, ''), @NameAr)),
        NULLIF(@Barcode, ''),
        0, 1, 2, 1, GETUTCDATE(), 0, 0
    );
    SET @targetMenuId = SCOPE_IDENTITY();
END

-- 5. Ensure branch InventoryItem exists for this raw material
IF @effBranchId > 0 AND @targetMenuId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM InventoryItems WHERE MenuSalesItemId = @targetMenuId AND BranchId = @effBranchId)
    BEGIN
        INSERT INTO InventoryItems (
            MenuSalesItemId, BranchId, CurrentQuantity, MinimumQuantity,
            TrackInventory, ItemNameAr, ItemNameEn, CreatedAt, ItemTypeId, ItemTypeCode
        ) VALUES (
            @targetMenuId, @effBranchId, 0, 0,
            1, @NameAr, ISNULL(NULLIF(@NameEn, ''), @NameAr), GETUTCDATE(), 2, 'RawMaterial'
        );
    END
END
";

                    using (var bridgeCmd = new SqlCommand(bridgeSql, localConn, transaction))
                    {
                        bridgeCmd.Parameters.AddWithValue("@Barcode", (object?)barcode ?? DBNull.Value);
                        bridgeCmd.Parameters.AddWithValue("@NameAr", nameAr);
                        bridgeCmd.Parameters.AddWithValue("@NameEn", (object?)nameEn ?? DBNull.Value);
                        bridgeCmd.Parameters.AddWithValue("@BranchId", localBranchId);
                        await bridgeCmd.ExecuteNonQueryAsync();
                    }
                }
            }

            // Sync Multi-units conversion table (WarehouseProductUnits)
            var checkPuCmd = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WarehouseProductUnits'", hqConn);
            if (Convert.ToInt32(await checkPuCmd.ExecuteScalarAsync()) > 0)
            {
                var puCmd = new SqlCommand("SELECT Id, ProductId, UnitId, ConversionFactorToBase, IsDefaultPurchaseUnit, IsDefaultRequisitionUnit FROM WarehouseProductUnits", hqConn);
                using (var puReader = await puCmd.ExecuteReaderAsync())
                {
                    var puDt = new DataTable();
                    puDt.Load(puReader);

                    foreach (DataRow row in puDt.Rows)
                    {
                        int id = Convert.ToInt32(row["Id"]);
                        int productId = Convert.ToInt32(row["ProductId"]);
                        int unitId = Convert.ToInt32(row["UnitId"]);
                        decimal factor = Convert.ToDecimal(row["ConversionFactorToBase"]);
                        bool isDefaultP = Convert.ToBoolean(row["IsDefaultPurchaseUnit"]);
                        bool isDefaultR = Convert.ToBoolean(row["IsDefaultRequisitionUnit"]);

                        var mergePu = @"
IF EXISTS (SELECT 1 FROM WarehouseProductUnits WHERE ProductId = @ProductId AND UnitId = @UnitId)
BEGIN
    UPDATE WarehouseProductUnits
    SET ConversionFactorToBase = @Factor,
        IsDefaultPurchaseUnit = @IsDefaultP,
        IsDefaultRequisitionUnit = @IsDefaultR
    WHERE ProductId = @ProductId AND UnitId = @UnitId;
END
ELSE
BEGIN
    SET IDENTITY_INSERT WarehouseProductUnits ON;
    INSERT INTO WarehouseProductUnits (Id, ProductId, UnitId, ConversionFactorToBase, IsDefaultPurchaseUnit, IsDefaultRequisitionUnit)
    VALUES (@Id, @ProductId, @UnitId, @Factor, @IsDefaultP, @IsDefaultR);
    SET IDENTITY_INSERT WarehouseProductUnits OFF;
END";
                        using (var puUpsert = new SqlCommand(mergePu, localConn, transaction))
                        {
                            puUpsert.Parameters.AddWithValue("@Id", id);
                            puUpsert.Parameters.AddWithValue("@ProductId", productId);
                            puUpsert.Parameters.AddWithValue("@UnitId", unitId);
                            puUpsert.Parameters.AddWithValue("@Factor", factor);
                            puUpsert.Parameters.AddWithValue("@IsDefaultP", isDefaultP);
                            puUpsert.Parameters.AddWithValue("@IsDefaultR", isDefaultR);
                            await puUpsert.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }
    }

    private async Task SyncSalesItemsIncremental(string hqConnString, SqlConnection localConn, SqlTransaction transaction, int localBranchId)
    {
        using (var hqConn = new SqlConnection(hqConnString))
        {
            await hqConn.OpenAsync();

            // 1. KitchenTypes (upsert by Id, preserve existing)
            await UpsertTableById(hqConn, localConn, transaction, "KitchenTypes", new[] { "KitchenName" });

            // 2. Classifications & Attributes
            await UpsertTableById(hqConn, localConn, transaction, "ItemsClassifications", new[] { "Name", "ArabicName", "Description" });
            await UpsertTableById(hqConn, localConn, transaction, "AttributeGroups", new[] { "Name", "ArabicName" });
            await UpsertTableById(hqConn, localConn, transaction, "Attributes", new[] { "Name", "ArabicName", "SelectionType", "AttributeGroupId" });
            await UpsertTableById(hqConn, localConn, transaction, "AttributeItems", new[] { "Name", "ArabicName", "Price", "AttributeId" });

            // 3. Categories (upsert by Id)
            await UpsertTableById(hqConn, localConn, transaction, "Categories", new[] { "ArabicName", "EnglishName", "Description", "ImagePath", "MainCategoryId", "BackColor", "TextColor", "TextSize", "Active" });

            // 4. MenuSalesItems: Match by Id, preserve local printers (KitchenTypeId, PrintInBackupReceipt)
            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MenuSalesItems'", hqConn);
            if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) == 0) return;

            var cmd = new SqlCommand(@"
SELECT Id, Barcode, ArabicName, EnglishName, NormalizedEnglishName, Price, SecondPrice, ThirdPrice,
       FourthPrice, FifthPrice, Tax, Description, ImagePath, MainCategoryId, CategoryId,
       BackColor, TextColor, TextSize, Invisible, HasAttribute, AttributeId, ByWeight,
       IsInventory, ItemTypeId, KitchenTypeId, PrintInBackupReceipt, CreationDate
FROM MenuSalesItems", hqConn);

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                var dt = new DataTable();
                dt.Load(reader);

                var upsertSql = @"
IF EXISTS (SELECT 1 FROM MenuSalesItems WHERE Id = @Id)
BEGIN
    UPDATE MenuSalesItems
    SET ArabicName = @ArabicName,
        EnglishName = @EnglishName,
        NormalizedEnglishName = @NormalizedEnglishName,
        Barcode = CASE WHEN (@Barcode IS NOT NULL AND @Barcode <> '') THEN @Barcode ELSE Barcode END,
        Price = @Price,
        SecondPrice = @SecondPrice,
        ThirdPrice = @ThirdPrice,
        FourthPrice = @FourthPrice,
        FifthPrice = @FifthPrice,
        Tax = @Tax,
        Description = @Description,
        ImagePath = @ImagePath,
        MainCategoryId = @MainCategoryId,
        CategoryId = @CategoryId,
        BackColor = @BackColor,
        TextColor = @TextColor,
        TextSize = @TextSize,
        Invisible = @Invisible,
        HasAttribute = @HasAttribute,
        AttributeId = @AttributeId,
        ByWeight = @ByWeight,
        IsInventory = @IsInventory,
        ItemTypeId = @ItemTypeId,
        UpdatedDate = GETUTCDATE()
        -- Note: KitchenTypeId and PrintInBackupReceipt are NOT overwritten to protect local branch printer setup!
    WHERE Id = @Id;
END
ELSE
BEGIN
    SET IDENTITY_INSERT MenuSalesItems ON;
    INSERT INTO MenuSalesItems (
        Id, Barcode, ArabicName, EnglishName, NormalizedEnglishName, Price, SecondPrice, ThirdPrice,
        FourthPrice, FifthPrice, Tax, Description, ImagePath, MainCategoryId, CategoryId,
        BackColor, TextColor, TextSize, Invisible, HasAttribute, AttributeId, ByWeight,
        IsInventory, ItemTypeId, KitchenTypeId, PrintInBackupReceipt, BranchId, CreationDate
    ) VALUES (
        @Id, @Barcode, @ArabicName, @EnglishName, @NormalizedEnglishName, @Price, @SecondPrice, @ThirdPrice,
        @FourthPrice, @FifthPrice, @Tax, @Description, @ImagePath, @MainCategoryId, @CategoryId,
        @BackColor, @TextColor, @TextSize, @Invisible, @HasAttribute, @AttributeId, @ByWeight,
        @IsInventory, @ItemTypeId, @KitchenTypeId, @PrintInBackupReceipt, @BranchId, @CreationDate
    );
    SET IDENTITY_INSERT MenuSalesItems OFF;
END";

                foreach (DataRow row in dt.Rows)
                {
                    using (var upsertCmd = new SqlCommand(upsertSql, localConn, transaction))
                    {
                        upsertCmd.Parameters.AddWithValue("@Id", Convert.ToInt32(row["Id"]));
                        upsertCmd.Parameters.AddWithValue("@Barcode", row["Barcode"] != DBNull.Value ? row["Barcode"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@ArabicName", row["ArabicName"]);
                        upsertCmd.Parameters.AddWithValue("@EnglishName", row["EnglishName"]);
                        upsertCmd.Parameters.AddWithValue("@NormalizedEnglishName", row["NormalizedEnglishName"] != DBNull.Value ? row["NormalizedEnglishName"] : (object)row["EnglishName"].ToString().ToUpperInvariant());
                        upsertCmd.Parameters.AddWithValue("@Price", row["Price"] != DBNull.Value ? row["Price"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@SecondPrice", row["SecondPrice"] != DBNull.Value ? row["SecondPrice"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@ThirdPrice", row["ThirdPrice"] != DBNull.Value ? row["ThirdPrice"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@FourthPrice", row["FourthPrice"] != DBNull.Value ? row["FourthPrice"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@FifthPrice", row["FifthPrice"] != DBNull.Value ? row["FifthPrice"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@Tax", row["Tax"] != DBNull.Value ? row["Tax"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@Description", row["Description"] != DBNull.Value ? row["Description"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@ImagePath", row["ImagePath"] != DBNull.Value ? row["ImagePath"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@MainCategoryId", row["MainCategoryId"] != DBNull.Value ? row["MainCategoryId"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@CategoryId", row["CategoryId"] != DBNull.Value ? row["CategoryId"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@BackColor", row["BackColor"] != DBNull.Value ? row["BackColor"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@TextColor", row["TextColor"] != DBNull.Value ? row["TextColor"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@TextSize", row["TextSize"] != DBNull.Value ? row["TextSize"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@Invisible", Convert.ToBoolean(row["Invisible"]));
                        upsertCmd.Parameters.AddWithValue("@HasAttribute", Convert.ToBoolean(row["HasAttribute"]));
                        upsertCmd.Parameters.AddWithValue("@AttributeId", row["AttributeId"] != DBNull.Value ? row["AttributeId"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@ByWeight", Convert.ToBoolean(row["ByWeight"]));
                        upsertCmd.Parameters.AddWithValue("@IsInventory", Convert.ToBoolean(row["IsInventory"]));
                        upsertCmd.Parameters.AddWithValue("@ItemTypeId", row["ItemTypeId"] != DBNull.Value ? row["ItemTypeId"] : 1);
                        upsertCmd.Parameters.AddWithValue("@KitchenTypeId", row["KitchenTypeId"] != DBNull.Value ? row["KitchenTypeId"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@PrintInBackupReceipt", row["PrintInBackupReceipt"] != DBNull.Value ? row["PrintInBackupReceipt"] : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@BranchId", localBranchId > 0 ? (object)localBranchId : DBNull.Value);
                        upsertCmd.Parameters.AddWithValue("@CreationDate", row["CreationDate"] != DBNull.Value ? row["CreationDate"] : DateTime.UtcNow);

                        await upsertCmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }
    }

    private async Task UpsertTableById(SqlConnection hqConn, SqlConnection localConn, SqlTransaction transaction, string tableName, string[] updateCols)
    {
        var checkCmd = new SqlCommand($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'", hqConn);
        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) == 0) return;

        var cmd = new SqlCommand($"SELECT * FROM [{tableName}]", hqConn);
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            var dt = new DataTable();
            dt.Load(reader);

            if (!dt.Columns.Contains("Id") || dt.Rows.Count == 0) return;

            var allCols = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            var validUpdateCols = updateCols.Where(c => allCols.Contains(c)).ToList();
            if (validUpdateCols.Count == 0) return;

            var setClause = string.Join(", ", validUpdateCols.Select(c => $"[{c}] = @{c}"));
            var insertCols = string.Join(", ", allCols.Select(c => $"[{c}]"));
            var insertParams = string.Join(", ", allCols.Select(c => $"@{c}"));

            var sql = $@"
IF EXISTS (SELECT 1 FROM [{tableName}] WHERE [Id] = @Id)
BEGIN
    UPDATE [{tableName}] SET {setClause} WHERE [Id] = @Id;
END
ELSE
BEGIN
    SET IDENTITY_INSERT [{tableName}] ON;
    INSERT INTO [{tableName}] ({insertCols}) VALUES ({insertParams});
    SET IDENTITY_INSERT [{tableName}] OFF;
END";

            foreach (DataRow row in dt.Rows)
            {
                using (var upsertCmd = new SqlCommand(sql, localConn, transaction))
                {
                    foreach (var col in allCols)
                    {
                        upsertCmd.Parameters.AddWithValue($"@{col}", row[col] ?? DBNull.Value);
                    }
                    await upsertCmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
