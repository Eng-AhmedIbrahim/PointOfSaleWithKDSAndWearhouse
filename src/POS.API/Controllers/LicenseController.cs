using Microsoft.AspNetCore.Mvc;
using Pos.Repository.Data;
using POS.Core.Entities.Settings;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace POS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LicenseController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const string CentralSecret = "POS_SYSTEM_v1_SECURE_KEY_2026_MASTER";
        private const string MasterSignKey = "NEW_TECH_HMAC_SIGNATURE_SECRET_v2_2026_ANTI_THEFT";
        public const string LicenseManagerAdminSecret = "LM_SUPER_ADMIN_AUTHORIZATION_TOKEN_SECRET_2026";

        public LicenseController(AppDbContext context)
        {
            _context = context;
        }

        public static string ComputeDeviceSignature(string hardwareId, string licenseKey, DateTime expiryDate)
        {
            var raw = $"{hardwareId.Trim().ToLower()}|{licenseKey.Trim()}|{expiryDate:yyyy-MM-dd HH:mm:ss}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(MasterSignKey));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        public static string ComputeLicenseRowSignature(string licenseKey, int customerId, int branchId, string dateLimit)
        {
            var raw = $"{licenseKey.Trim()}|{customerId}|{branchId}|{dateLimit.Trim()}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(MasterSignKey));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        [HttpPost("RegisterDevice")]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
        {
            // 1. Decrypt License Key
            var info = DecryptKey(request.LicenseKey);
            if (info == null)
            {
                return BadRequest("مفتاح الترخيص غير صالح.");
            }

            int branchId = info.Value.BranchId;
            int maxDevices = info.Value.MaxDevices;
            string licenseTypeStr = info.Value.Type switch { 1 => "POSOnly", 2 => "BackOfficeOnly", 3 => "Full", 4 => "WarehouseOnly", _ => "POSOnly" };
            
            // Validate Expiry
            if (DateTime.Now > info.Value.ExpiryDate)
            {
                return BadRequest("مفتاح الترخيص منتهي الصلاحية.");
            }

            // 2a. SECURITY: Prevent device from registering to multiple branches
            var deviceInAnyBranch = await _context.LicensedDevices
                .FirstOrDefaultAsync(d => d.HardwareId == request.HardwareId && d.BranchId != branchId);

            if (deviceInAnyBranch != null)
            {
                return BadRequest("هذا الجهاز مسجل بالفعل في فرع آخر. يجب فك الارتباط أولاً قبل التسجيل في فرع جديد.");
            }

            // 2b. Check existing devices for this specific license key
            // Count is scoped per license key (not per branch+type) so each key has its own independent quota
            var activeDevicesCount = await _context.LicensedDevices
                .CountAsync(d => d.BranchId == branchId && d.LicenseType == licenseTypeStr && d.LicenseKey == request.LicenseKey);

            // 2c. ANTI-CLONE CHECK: Verify that the LicenseKey exists in Licenses table and its signature is authentic
            var licenseRecord = await _context.Licenses
                .FirstOrDefaultAsync(l => l.LicenseKey == request.LicenseKey && l.BranchID == branchId);

            if (licenseRecord == null)
            {
                return BadRequest("مفتاح الترخيص غير مسجل في قاعدة البيانات لهذا الفرع.");
            }

            if (!string.IsNullOrEmpty(licenseRecord.Signature))
            {
                var expectedLicSig = ComputeLicenseRowSignature(licenseRecord.LicenseKey, licenseRecord.CustomerID, licenseRecord.BranchID, licenseRecord.DateLimit ?? "");
                if (licenseRecord.Signature != expectedLicSig)
                {
                    return BadRequest("تم اكتشاف نسخ غير شرعي للترخيص من قاعدة بيانات أخرى.");
                }
            }

            var existingDevice = await _context.LicensedDevices
                .FirstOrDefaultAsync(d => d.HardwareId == request.HardwareId && d.BranchId == branchId && d.LicenseKey == request.LicenseKey);

            if (existingDevice != null)
            {
                // Already registered in this branch; update expiry if needed
                existingDevice.ExpiryDate = info.Value.ExpiryDate;
                existingDevice.LicenseType = licenseTypeStr;
                existingDevice.Signature = ComputeDeviceSignature(existingDevice.HardwareId, existingDevice.LicenseKey ?? request.LicenseKey, existingDevice.ExpiryDate);

                // Also update Licenses table if exists
                var existingLicense = await _context.Licenses
                    .FirstOrDefaultAsync(l => l.LicenseKey == request.LicenseKey && l.BranchID == branchId);
                
                if (existingLicense != null)
                {
                    existingLicense.ComputerName = Environment.MachineName;
                    existingLicense.MacAddress = request.HardwareId;
                    existingLicense.JoinDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Ok(new { Message = "تم تحديث ترخيص الجهاز بنجاح." });
            }

            if (activeDevicesCount >= maxDevices)
            {
                return BadRequest("لقد تجاوزت الحد الأقصى للأجهزة المسموح بها لهذا الفرع.");
            }

            // 3. Register New Device with HMAC Digital Signature
            var newDevice = new LicensedDevice
            {
                HardwareId = request.HardwareId,
                BranchId = branchId,
                ActivationDate = DateTime.Now,
                ExpiryDate = info.Value.ExpiryDate,
                LicenseType = licenseTypeStr,
                LicenseKey = request.LicenseKey,
                Signature = ComputeDeviceSignature(request.HardwareId, request.LicenseKey, info.Value.ExpiryDate)
            };

            _context.LicensedDevices.Add(newDevice);

            // Also update Licenses table
            if (licenseRecord != null)
            {
                licenseRecord.ComputerName = Environment.MachineName;
                licenseRecord.MacAddress = request.HardwareId;
                licenseRecord.JoinDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم تسجيل الجهاز بنجاح ضمن العدد المسموح بيه." });
        }

        [HttpGet("GenerateKey")]
        public IActionResult GenerateKey(int branchId = 1, int licenseType = 1, int maxDevices = 3, int expiryMonths = 12)
        {
            // SECURITY: Require Secret Header so only LicenseManager can generate keys
            var adminSecret = Request.Headers["X-License-Manager-Secret"].ToString();
            if (adminSecret != LicenseManagerAdminSecret)
            {
                return StatusCode(403, new { Message = "غير مصرح بتوليد التراخيص إلا من خلال تطبيق License Manager المعتمد." });
            }

            // licenseType: 0 = POSOnly, 1 = Full
            var payload = new
            {
                BranchId = branchId,
                Type = licenseType,
                MaxDevices = maxDevices,
                ExpiryDate = DateTime.Now.AddMonths(expiryMonths)
            };

            string json = JsonSerializer.Serialize(payload);

            using var aes = Aes.Create();
            using var sha256 = SHA256.Create();
            aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(CentralSecret));
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(json);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            
            byte[] result = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
            
            string base64String = Convert.ToBase64String(result);

            // Log to Licenses table
            var branch = _context.Branches.Include(b => b.Company).FirstOrDefault(b => b.Id == branchId);
            
            var dateLimit = payload.ExpiryDate.ToString("yyyyMMddhhmmss.fff");
            var custId = branch?.CompanyId ?? 1;

            var newLicense = new POS.Core.Entities.Settings.License
            {
                CustomerID = custId,
                CustomerName = branch?.Company?.ArabicName ?? (branch != null ? "Backend" : "Unknown"),
                BranchID = branchId,
                BranchName = branch?.Name ?? "Main Branch",
                LicenseKey = base64String,
                GenerateDate = DateTime.Now.ToString("MMM dd yyyy"),
                DateLimit = dateLimit,
                LicenseType = licenseType switch { 1 => "POSOnly", 2 => "BackOfficeOnly", 3 => "Full", 4 => "WarehouseOnly", _ => "POSOnly" },
                CreationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Signature = ComputeLicenseRowSignature(base64String, custId, branchId, dateLimit)
            };
            
            _context.Licenses.Add(newLicense);
            _context.SaveChanges();

            string typeName = licenseType switch { 1 => "POS Only", 2 => "BackOffice Only", 3 => "Full (Back+Front)", 4 => "Warehouse Only", _ => "POS Only" };

            return Ok(new { 
                Message = "احتفظ بهذا المفتاح، وقم بإدخاله في شاشة التفعيل الخاصة بالفرع.",
                BranchId = branchId,
                MaxDevices = maxDevices,
                LicenseType = typeName,
                ExpiryDate = payload.ExpiryDate.ToString("yyyy-MM-dd"),
                LicenseKey = base64String 
            });
        }

        private (int BranchId, int Type, int MaxDevices, DateTime ExpiryDate)? DecryptKey(string key)

        {
            try
            {
                byte[] encrypted = Convert.FromBase64String(key);
                using var aes = Aes.Create();
                using var sha256 = SHA256.Create();
                aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(CentralSecret));
                byte[] iv = new byte[16];
                Buffer.BlockCopy(encrypted, 0, iv, 0, 16);
                aes.IV = iv;
                using var decryptor = aes.CreateDecryptor();
                byte[] encryptedBytes = new byte[encrypted.Length - 16];
                Buffer.BlockCopy(encrypted, 16, encryptedBytes, 0, encryptedBytes.Length);
                byte[] plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                string json = Encoding.UTF8.GetString(plainBytes);

                var payload = JsonSerializer.Deserialize<JsonElement>(json);
                return (
                    payload.GetProperty("BranchId").GetInt32(),
                    payload.GetProperty("Type").GetInt32(),
                    payload.GetProperty("MaxDevices").GetInt32(),
                    payload.GetProperty("ExpiryDate").GetDateTime()
                );
            }
            catch
            {
                return null; // Invalid key
            }
        }

        [HttpGet("GetBranches")]
        public async Task<IActionResult> GetBranches()
        {
            var branches = await _context.Branches
                .Include(b => b.Company)
                .Select(b => new 
                {
                    b.Id,
                    BranchName = b.Name,
                    CompanyId = b.CompanyId,
                    CompanyName = b.Company != null ? b.Company.ArabicName : "N/A"
                })
                .ToListAsync();

            return Ok(branches);
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var licenses = await _context.Licenses.ToListAsync();
            return Ok(licenses);
        }

        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            try
            {
                bool canConnect = await _context.Database.CanConnectAsync();
                if (canConnect)
                {
                    return Ok(new { status = "Healthy", database = true });
                }
                return StatusCode(503, new { status = "Unhealthy", database = false, message = "تعذر الاتصال بقاعدة البيانات" });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { status = "Unhealthy", database = false, message = ex.Message });
            }
        }

        [HttpGet("CheckDevice")]
        public async Task<IActionResult> CheckDevice([FromQuery] string hardwareId, [FromQuery] string? scope = null)
        {
            if (string.IsNullOrWhiteSpace(hardwareId))
                return BadRequest("HardwareId is required.");

            IQueryable<LicensedDevice> query = _context.LicensedDevices
                .Where(d => d.HardwareId == hardwareId && d.ExpiryDate > DateTime.Now);

            if (!string.IsNullOrEmpty(scope))
            {
                if (scope.Equals("Warehouse", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(d => d.LicenseType == "WarehouseOnly" || d.LicenseType == "4");
                }
                else if (scope.Equals("BackOffice", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(d => d.LicenseType == "BackOfficeOnly" || d.LicenseType == "2" || d.LicenseType == "Full" || d.LicenseType == "3");
                }
                else if (scope.Equals("POS", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(d => d.LicenseType == "POSOnly" || d.LicenseType == "1" || d.LicenseType == "Full" || d.LicenseType == "3");
                }
            }

            var device = await query.FirstOrDefaultAsync();

            if (device == null)
                return Ok(new { Licensed = false, LicenseType = (string?)null });

            // ANTI-TAMPER CHECK: Verify HMAC Signature to prevent manual DB edits
            if (!string.IsNullOrEmpty(device.Signature))
            {
                var expectedSig = ComputeDeviceSignature(device.HardwareId, device.LicenseKey ?? "", device.ExpiryDate);
                if (device.Signature != expectedSig)
                {
                    // Tampered manually in SQL!
                    return Ok(new { Licensed = false, LicenseType = (string?)null, Message = "تم اكتشاف تلاعب أمني في قاعدة البيانات. الجهاز غير مرخص." });
                }
            }

            // Normalize: handles both old numeric ("1","2","3","4") and new string format
            string normalizedType = device.LicenseType switch
            {
                "1" or "POSOnly"           => "POSOnly",
                "2" or "BackOfficeOnly"    => "BackOfficeOnly",
                "3" or "Full"              => "Full",
                "4" or "WarehouseOnly"     => "WarehouseOnly",
                _ => device.LicenseType
            };

            return Ok(new { Licensed = true, LicenseType = normalizedType });
        }

        [HttpPost("UnlinkDevice")]
        public async Task<IActionResult> UnlinkDevice([FromBody] UnlinkDeviceRequest request)
        {
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.LicenseKey == request.LicenseKey && l.BranchID == request.BranchId);

            if (license == null)
            {
                return NotFound("الترخيص غير موجود.");
            }

            // Reset License hardware fields
            string oldMac = license.MacAddress!;
            license.MacAddress = null;
            license.ComputerName = null;
            license.JoinDate = null;

            // Remove from LicensedDevices
            if (!string.IsNullOrEmpty(oldMac))
            {
                var licensedDevice = await _context.LicensedDevices
                    .FirstOrDefaultAsync(d => d.HardwareId == oldMac && d.BranchId == request.BranchId);
                    
                if (licensedDevice != null)
                {
                    _context.LicensedDevices.Remove(licensedDevice);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "تم فك ارتباط الجهاز بالترخيص بنجاح." });
        }

        [HttpGet("GetOrderCounterStatus")]
        public async Task<IActionResult> GetOrderCounterStatus([FromQuery] int branchId = 1)
        {
            var appDate = await _context.AppDate.FirstOrDefaultAsync(a => a.BranchId == branchId) 
                       ?? await _context.AppDate.FirstOrDefaultAsync();

            var setting = await _context.PosFeatureSettings.FirstOrDefaultAsync(s => s.FeatureName == "AutoResetOrderLimit");
            bool isAutoResetEnabled = setting != null && setting.Value;
            int maxOrderLimit = 100;
            if (setting != null && int.TryParse(setting.ScreenName, out int parsedLimit) && parsedLimit > 0)
            {
                maxOrderLimit = parsedLimit;
            }

            bool triggerInstalled = false;
            try
            {
                var connection = _context.Database.GetDbConnection();
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sys.triggers WHERE name = 'trg_AutoResetOrderNumber'";
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                triggerInstalled = Convert.ToInt32(result) > 0;
            }
            catch { }

            return Ok(new
            {
                BranchId = appDate?.BranchId ?? branchId,
                PosDate = appDate?.PosDate.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"),
                StoreDate = appDate?.StoreDate.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"),
                CurrentOrderNumber = appDate?.CurrentOrderNumber ?? 0,
                IsAutoResetEnabled = isAutoResetEnabled,
                MaxOrderLimit = maxOrderLimit,
                TriggerInstalled = triggerInstalled
            });
        }

        [HttpPost("ConfigureOrderReset")]
        public async Task<IActionResult> ConfigureOrderReset([FromBody] ConfigureOrderResetRequest request)
        {
            var appDate = await _context.AppDate.FirstOrDefaultAsync(a => a.BranchId == request.BranchId)
                       ?? await _context.AppDate.FirstOrDefaultAsync();

            if (appDate == null)
            {
                return BadRequest("لم يتم العثور على سجل تاريخ التطبيق (AppDate) في قاعدة البيانات.");
            }

            // 1. Reset current counter if requested
            if (request.ResetCurrentNow)
            {
                appDate.CurrentOrderNumber = 0;
                _context.AppDate.Update(appDate);
            }
            else if (request.SetCurrentTo.HasValue)
            {
                appDate.CurrentOrderNumber = request.SetCurrentTo.Value;
                _context.AppDate.Update(appDate);
            }

            // 2. Save / Update Setting in PosFeatureSettings
            var setting = await _context.PosFeatureSettings.FirstOrDefaultAsync(s => s.FeatureName == "AutoResetOrderLimit");
            if (setting == null)
            {
                setting = new POS.Core.Entities.OrderEntity.PosFeatureSetting
                {
                    FeatureName = "AutoResetOrderLimit",
                    NameAr = "تصفير عداد الأوردرات بعد حد معين",
                    NameEn = "Auto Reset Order Limit",
                    ModuleName = "Orders",
                    ScreenName = request.MaxOrderLimit.ToString(),
                    Value = request.EnableAutoReset
                };
                await _context.PosFeatureSettings.AddAsync(setting);
            }
            else
            {
                setting.Value = request.EnableAutoReset;
                setting.ScreenName = request.MaxOrderLimit.ToString();
                _context.PosFeatureSettings.Update(setting);
            }

            await _context.SaveChangesAsync();

            // 3. Execute Database Script (Create or Drop Trigger)
            string scriptStatus = "غير مفعل";
            try
            {
                if (request.EnableAutoReset && request.MaxOrderLimit > 0)
                {
                    // Drop old trigger if exists
                    await _context.Database.ExecuteSqlRawAsync(@"
                        IF OBJECT_ID('dbo.trg_AutoResetOrderNumber', 'TR') IS NOT NULL
                            DROP TRIGGER dbo.trg_AutoResetOrderNumber;
                    ");

                    // Create new trigger
                    var createTriggerSql = $@"
                    CREATE TRIGGER dbo.trg_AutoResetOrderNumber
                    ON dbo.AppDates
                    AFTER UPDATE
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        IF UPDATE(CurrentOrderNumber)
                        BEGIN
                            DECLARE @maxLimit INT = {request.MaxOrderLimit};
                            IF @maxLimit > 0
                            BEGIN
                                UPDATE a
                                SET CurrentOrderNumber = 0
                                FROM dbo.AppDates a
                                INNER JOIN inserted i ON a.Id = i.Id
                                WHERE i.CurrentOrderNumber > @maxLimit;
                            END
                        END
                    END";

                    await _context.Database.ExecuteSqlRawAsync(createTriggerSql);
                    scriptStatus = $"✅ تم إنشاء وتفعيل اسكريبت التريجر في الداتا بيز بعد كل {request.MaxOrderLimit} أوردر.";
                }
                else
                {
                    await _context.Database.ExecuteSqlRawAsync(@"
                        IF OBJECT_ID('dbo.trg_AutoResetOrderNumber', 'TR') IS NOT NULL
                            DROP TRIGGER dbo.trg_AutoResetOrderNumber;
                    ");
                    scriptStatus = "❌ تم إلغاء تفعيل الاسكريبت وحذف التريجر من قاعدة البيانات.";
                }
            }
            catch (Exception ex)
            {
                scriptStatus = $"⚠️ تم حفظ الإعداد البرمجي لكن تعذر تطبيق التريجر في SQL Server: {ex.Message}";
            }

            return Ok(new
            {
                Success = true,
                Message = $"تم حفظ الإعدادات بنجاح. {scriptStatus}",
                CurrentOrderNumber = appDate.CurrentOrderNumber,
                IsAutoResetEnabled = request.EnableAutoReset,
                MaxOrderLimit = request.MaxOrderLimit,
                ScriptStatus = scriptStatus
            });
        }
    }

    public class UnlinkDeviceRequest
    {
        public string LicenseKey { get; set; } = string.Empty;
        public int BranchId { get; set; }
    }

    public class RegisterDeviceRequest
    {
        public string HardwareId { get; set; } = string.Empty;
        public string LicenseKey { get; set; } = string.Empty;
    }

    public class ConfigureOrderResetRequest
    {
        public int BranchId { get; set; } = 1;
        public bool EnableAutoReset { get; set; }
        public int MaxOrderLimit { get; set; } = 100;
        public bool ResetCurrentNow { get; set; }
        public int? SetCurrentTo { get; set; }
    }
}
