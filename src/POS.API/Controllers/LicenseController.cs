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

        public LicenseController(AppDbContext context)
        {
            _context = context;
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
            string licenseTypeStr = info.Value.Type switch { 1 => "POSOnly", 2 => "BackOfficeOnly", 3 => "Full", _ => "POSOnly" };
            
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

            var existingDevice = await _context.LicensedDevices
                .FirstOrDefaultAsync(d => d.HardwareId == request.HardwareId && d.BranchId == branchId && d.LicenseKey == request.LicenseKey);

            if (existingDevice != null)
            {
                // Already registered in this branch; update expiry if needed
                existingDevice.ExpiryDate = info.Value.ExpiryDate;
                existingDevice.LicenseType = licenseTypeStr;

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

            // 3. Register New Device
            var newDevice = new LicensedDevice
            {
                HardwareId = request.HardwareId,
                BranchId = branchId,
                ActivationDate = DateTime.Now,
                ExpiryDate = info.Value.ExpiryDate,
                LicenseType = licenseTypeStr,
                LicenseKey = request.LicenseKey
            };

            _context.LicensedDevices.Add(newDevice);

            // Also update Licenses table
            var licenseRecord = await _context.Licenses
                .FirstOrDefaultAsync(l => l.LicenseKey == request.LicenseKey && l.BranchID == branchId);
                
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
            
            var newLicense = new POS.Core.Entities.Settings.License
            {
                CustomerID = branch?.CompanyId ?? 1,
                CustomerName = branch?.Company?.ArabicName ?? (branch != null ? "Backend" : "Unknown"),
                BranchID = branchId,
                BranchName = branch?.Name ?? "Main Branch",
                LicenseKey = base64String,
                GenerateDate = DateTime.Now.ToString("MMM dd yyyy"),
                DateLimit = payload.ExpiryDate.ToString("yyyyMMddhhmmss.fff"),
                LicenseType = licenseType switch { 1 => "POSOnly", 2 => "BackOfficeOnly", 3 => "Full", _ => "POSOnly" },
                CreationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            _context.Licenses.Add(newLicense);
            _context.SaveChanges();

            string typeName = licenseType switch { 1 => "POS Only", 2 => "BackOffice Only", 3 => "Full (Back+Front)", _ => "POS Only" };

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

        [HttpGet("CheckDevice")]
        public async Task<IActionResult> CheckDevice([FromQuery] string hardwareId)
        {
            if (string.IsNullOrWhiteSpace(hardwareId))
                return BadRequest("HardwareId is required.");

            var device = await _context.LicensedDevices
                .FirstOrDefaultAsync(d => d.HardwareId == hardwareId && d.ExpiryDate > DateTime.Now);

            if (device == null)
                return Ok(new { Licensed = false, LicenseType = (string?)null });

            // Normalize: handles both old numeric ("1","2","3") and new string format
            string normalizedType = device.LicenseType switch
            {
                "1" or "POSOnly"       => "POSOnly",
                "2" or "BackOfficeOnly" => "BackOfficeOnly",
                "3" or "Full"          => "Full",
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
}
