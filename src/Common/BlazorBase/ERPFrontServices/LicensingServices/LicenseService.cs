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
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (bool Licensed, string? LicenseType, DateTime Expiry)> _scopeCache = new();
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
            var res = await VerifyDeviceLicenseAsync("POS");
            return (res.IsLicensed, res.LicenseType);
        }

        public async Task<(bool IsConnected, string? ErrorMessage)> CheckHealthAsync()
        {
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(3));
                var response = await _httpClient.GetAsync("api/License/health", cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }
                
                if ((int)response.StatusCode == 503)
                {
                    return (false, "تم الاتصال بالخادم (API)، ولكن تعذر الاتصال بقاعدة البيانات. يرجى التأكد من تشغيل خدمة SQL Server.");
                }

                return (false, $"استجاب الخادم برمز خطأ: {(int)response.StatusCode}");
            }
            catch (HttpRequestException)
            {
                return (false, "تعذر الوصول إلى سيرفر النظام (API). يرجى التأكد من تشغيل السيرفر والاتصال بالشبكة.");
            }
            catch (TaskCanceledException)
            {
                return (false, "انتهت مهلة الاتصال بسيرفر النظام (Timeout). يرجى فحص الشبكة وتشغيل السيرفر.");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ في الاتصال بالخادم: {ex.Message}");
            }
        }

        public async Task<LicenseVerificationResult> VerifyDeviceLicenseAsync(string requiredScope = "POS")
        {
            // Return cached result if still valid for this specific scope
            if (_scopeCache.TryGetValue(requiredScope, out var cached) && DateTime.Now < cached.Expiry)
            {
                var cachedType = cached.LicenseType;
                bool auth = requiredScope switch
                {
                    "BackOffice" => cachedType == "BackOfficeOnly" || cachedType == "Full",
                    "Warehouse" => cachedType == "WarehouseOnly",
                    "POS" => cachedType == "POSOnly" || cachedType == "Full",
                    _ => cachedType == "Full"
                };

                if (auth)
                {
                    return new LicenseVerificationResult
                    {
                        Status = LicenseCheckStatus.Valid,
                        LicenseType = cachedType
                    };
                }
            }

            // 1. Health check first: is API and Database reachable?
            var (isConnected, errorMsg) = await CheckHealthAsync();
            if (!isConnected)
            {
                var isDbDown = errorMsg != null && errorMsg.Contains("قاعدة البيانات");
                return new LicenseVerificationResult
                {
                    Status = isDbDown ? LicenseCheckStatus.DatabaseUnavailable : LicenseCheckStatus.ApiUnreachable,
                    ErrorMessage = errorMsg
                };
            }

            // 2. Query device license from API scoped
            try
            {
                var hardwareId = await GetHardwareIdAsync();
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(4));
                var response = await _httpClient.GetAsync(
                    $"api/License/CheckDevice?hardwareId={Uri.EscapeDataString(hardwareId)}&scope={Uri.EscapeDataString(requiredScope)}",
                    cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _scopeCache[requiredScope] = (false, null, DateTime.Now.AddSeconds(15));
                    return new LicenseVerificationResult
                    {
                        Status = LicenseCheckStatus.Unlicensed,
                        ErrorMessage = "تعذر التحقق من ترخيص الجهاز."
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<CheckDeviceResult>();
                if (result == null || !result.Licensed || string.IsNullOrEmpty(result.LicenseType))
                {
                    _scopeCache[requiredScope] = (false, null, DateTime.Now.AddSeconds(15));
                    return new LicenseVerificationResult
                    {
                        Status = LicenseCheckStatus.Unlicensed,
                        ErrorMessage = "هذا الجهاز غير مرخص. يرجى تسجيل الجهاز وتفعيله من خلال برنامج الإدارة قبل الاستخدام."
                    };
                }

                // Check scope authorization
                bool isAuthorized = requiredScope switch
                {
                    "BackOffice" => result.LicenseType == "BackOfficeOnly" || result.LicenseType == "Full",
                    "Warehouse" => result.LicenseType == "WarehouseOnly",
                    "POS" => result.LicenseType == "POSOnly" || result.LicenseType == "Full",
                    _ => result.LicenseType == "Full"
                };

                if (!isAuthorized)
                {
                    _scopeCache[requiredScope] = (false, result.LicenseType, DateTime.Now.AddSeconds(15));
                    return new LicenseVerificationResult
                    {
                        Status = LicenseCheckStatus.Unlicensed,
                        LicenseType = result.LicenseType,
                        ErrorMessage = $"نوع ترخيص هذا الجهاز ({result.LicenseType}) لا يسمح باستخدام {requiredScope}."
                    };
                }

                _scopeCache[requiredScope] = (true, result.LicenseType, DateTime.Now.Add(CacheDuration));

                return new LicenseVerificationResult
                {
                    Status = LicenseCheckStatus.Valid,
                    LicenseType = result.LicenseType
                };
            }
            catch (Exception ex)
            {
                return new LicenseVerificationResult
                {
                    Status = LicenseCheckStatus.ApiUnreachable,
                    ErrorMessage = $"تعذر الاتصال بالخادم: {ex.Message}"
                };
            }
        }

        private class CheckDeviceResult
        {
            public bool Licensed { get; set; }
            public string? LicenseType { get; set; }
        }

        public async Task<bool> IsLicensedAsync()
        {
            var res = await VerifyDeviceLicenseAsync("POS");
            return res.IsLicensed;
        }

        public async Task<bool> IsBackOfficeAuthorizedAsync()
        {
            var res = await VerifyDeviceLicenseAsync("BackOffice");
            return res.IsLicensed;
        }

        public async Task<bool> IsWarehouseAuthorizedAsync()
        {
            var res = await VerifyDeviceLicenseAsync("Warehouse");
            return res.IsLicensed;
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

            _scopeCache.Clear();

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
