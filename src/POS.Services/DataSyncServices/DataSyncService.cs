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

                    if (request.SyncSalesItems)
                    {
                        tablesToSync.AddRange(new[] 
                        { 
                            "KitchenTypes",         
                            "ItemsClassifications", 
                            "Attributes", 
                            "AttributeGroups", 
                            "Categories", 
                            "MenuSalesItems",       
                            "AttributeItems" 
                        });
                    }

                    // Perform Sync (Overwrite with BranchId mapping)
                    foreach (var table in tablesToSync)
                    {
                        await SyncTable(hqConnString, table, localConn, transaction, localBranchId);
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
}
