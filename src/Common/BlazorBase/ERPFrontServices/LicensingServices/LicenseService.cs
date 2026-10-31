using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using BlazorBase.API;
using Microsoft.Extensions.Configuration;
using System.Management; // Usually needs NuGet System.Management

namespace BlazorBase.ERPFrontServices.LicensingServices
{
    public class LicenseService : ILicenseService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _licenseFilePath;
        private const string InternalSecret = "POS_SYSTEM_v1_SECURE_KEY_2026";

        // Cache to avoid repeated API calls on every check
        private static (bool Licensed, string? LicenseType)? _cachedResult = null;
        private static DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        private static string? _cachedHardwareId = null;

        public LicenseService(IConfiguration configuration, IHttpClientFactory httpClientFactory, BlazorBase.API.ApiSettings apiSettings)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName ?? "Default");
            _httpClient.Timeout = TimeSpan.FromSeconds(5); // Fast fail if API unreachable
            var sharedFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "StorePOS");
            Directory.CreateDirectory(sharedFolder);
            _licenseFilePath = Path.Combine(sharedFolder, "license.dat");
        }


        private async Task<bool> IsValidHardwareLicenseAsync(LicenseInfo? info)
        {
            if (info == null) return false;

            // 1. Verify Hardware ID matches
            var currentHardwareId = await GetHardwareIdAsync();
            if (info.HardwareId != currentHardwareId) return false;

            // 2. Check Expiry
            if (DateTime.Now > info.ExpiryDate) return false;

            return true;
        }

        // Check device license directly from the API (database) with caching
        private async Task<(bool Licensed, string? LicenseType)> CheckDeviceFromApiAsync()
        {
            // Return cached result if still valid
            if (_cachedResult.HasValue && DateTime.Now < _cacheExpiry)
                return _cachedResult.Value;

            try
            {
                var hardwareId = await GetHardwareIdAsync();
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(4));
                var response = await _httpClient.GetAsync(
                    $"api/License/CheckDevice?hardwareId={Uri.EscapeDataString(hardwareId)}",
                    cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _cachedResult = (false, null);
                    _cacheExpiry = DateTime.Now.AddSeconds(30); // retry sooner on failure
                    return _cachedResult.Value;
                }

                var result = await response.Content.ReadFromJsonAsync<CheckDeviceResult>();
                _cachedResult = (result?.Licensed ?? false, result?.LicenseType);
                _cacheExpiry = DateTime.Now.Add(CacheDuration);
                return _cachedResult.Value;
            }
            catch
            {
                // If API unreachable, use last cached value if exists, else deny
                if (_cachedResult.HasValue)
                    return _cachedResult.Value;
                return (false, null);
            }
        }

        private class CheckDeviceResult
        {
            public bool Licensed { get; set; }
            public string? LicenseType { get; set; }
        }

        public async Task<bool> IsLicensedAsync()
        {
            var (licensed, licenseType) = await CheckDeviceFromApiAsync();
            if (!licensed || licenseType == null) return false;

            // POSOnly (1) or Full (3)
            return licenseType == "POSOnly" || licenseType == "Full";
        }

        public async Task<bool> IsBackOfficeAuthorizedAsync()
        {
            var (licensed, licenseType) = await CheckDeviceFromApiAsync();
            if (!licensed || licenseType == null) return false;

            // BackOfficeOnly (2) or Full (3)
            return licenseType == "BackOfficeOnly" || licenseType == "Full";
        }

        public Task<string> GetHardwareIdAsync()
        {
            // Cache hardware ID - it never changes during runtime
            if (_cachedHardwareId != null)
                return Task.FromResult(_cachedHardwareId);

            try
            {
                string cpuId = string.Empty;
                string mbId = string.Empty;

                // Getting CPU ID
                using (var mc = new ManagementClass("win32_processor"))
                {
                    var moc = mc.GetInstances();
                    foreach (var mo in moc)
                    {
                        cpuId = mo.Properties["ProcessorId"].Value?.ToString() ?? "";
                        break;
                    }
                }

                // Getting Motherboard Serial
                using (var mc = new ManagementClass("win32_baseboard"))
                {
                    var moc = mc.GetInstances();
                    foreach (var mo in moc)
                    {
                        mbId = mo.Properties["SerialNumber"].Value?.ToString() ?? "";
                        break;
                    }
                }

                string combined = $"{cpuId}-{mbId}-{InternalSecret}";
                _cachedHardwareId = ComputeHash(combined);
                return Task.FromResult(_cachedHardwareId);
            }
            catch
            {
                // Fallback for environments where ManagementClass might fail
                _cachedHardwareId = ComputeHash(Environment.MachineName + InternalSecret);
                return Task.FromResult(_cachedHardwareId);
            }
        }

        public bool VerifyAdminCredentials(string username, string password)
        {
            string usernameHash = ComputeHash(username);
            string passwordHash = ComputeHash(password);

            var configUserHash = _configuration["LicensingAdmin:UsernameHash"];
            var configPassHash = _configuration["LicensingAdmin:PasswordHash"];

            return usernameHash == configUserHash && passwordHash == configPassHash;
        }

        public async Task<(bool Success, string Message)> ActivateAsync(ActivationRequest request)
        {
            // 1. Verify Admin Credentials
            if (!VerifyAdminCredentials(request.Username, request.Password))
            {
                return (false, "بيانات الأدمن غير صحيحة.");
            }

            if (string.IsNullOrWhiteSpace(request.LicenseKey))
            {
                return (false, "مفتاح الترخيص (License Key) مطلوب.");
            }

            // 2. Decode License Key to check format locally
            var decoded = LicenseKeyGenerator.DecryptKey(request.LicenseKey);
            if (decoded == null)
            {
                return (false, "مفتاح الترخيص غير صالح.");
            }

            var hardwareId = await GetHardwareIdAsync();

            // 3. Register Device with API
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsJsonAsync("api/License/RegisterDevice", new 
                {
                    HardwareId = hardwareId,
                    LicenseKey = request.LicenseKey
                });
            }
            catch (Exception ex)
            {
                return (false, "فشل الاتصال بالخادم الداخلي للتحقق من الترخيص. يرجى التأكد من تشغيل الخادم.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(errorContent) ? "تجاوز الحد الأقصى للأجهزة أو مفتاح الترخيص مرفوض." : errorContent);
            }

            // 4. Generate local License Info
            var licenseInfo = new LicenseInfo
            {
                HardwareId = hardwareId,
                LicenseKey = request.LicenseKey,
                Type = decoded.Value.Type,
                BranchId = decoded.Value.BranchId,
                ExpiryDate = decoded.Value.ExpiryDate,
                MaxDeviceCount = decoded.Value.MaxDevices
            };

            // 5. Save Encrypted offline
            await SaveLicenseAsync(licenseInfo);

            return (true, "تم تفعيل الجهاز وتسجيله في الفرع بنجاح.");
        }


        public async Task<LicenseInfo?> GetLicenseInfoAsync()
        {
            if (!File.Exists(_licenseFilePath)) return null;

            try
            {
                byte[] encryptedData = await File.ReadAllBytesAsync(_licenseFilePath);
                string hardwareId = await GetHardwareIdAsync();
                
                string json = Decrypt(encryptedData, hardwareId + InternalSecret);
                var info = JsonSerializer.Deserialize<LicenseInfo>(json);
                
                return info;
            }
            catch
            {
                return null;
            }
        }

        private async Task SaveLicenseAsync(LicenseInfo info)
        {
            string json = JsonSerializer.Serialize(info);
            byte[] encryptedData = Encrypt(json, info.HardwareId + InternalSecret);
            await File.WriteAllBytesAsync(_licenseFilePath, encryptedData);
        }

        private string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        private byte[] Encrypt(string text, string key)
        {
            using var aes = Aes.Create();
            aes.Key = DeriveKey(key);
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(text);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            byte[] result = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
            return result;
        }

        private string Decrypt(byte[] data, string key)
        {
            using var aes = Aes.Create();
            aes.Key = DeriveKey(key);
            
            byte[] iv = new byte[16];
            Buffer.BlockCopy(data, 0, iv, 0, 16);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            byte[] encryptedBytes = new byte[data.Length - 16];
            Buffer.BlockCopy(data, 16, encryptedBytes, 0, encryptedBytes.Length);
            
            byte[] plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private byte[] DeriveKey(string key)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        }
    }
}
