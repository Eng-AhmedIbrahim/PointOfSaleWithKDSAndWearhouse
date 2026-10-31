using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BlazorBase.ERPFrontServices.LicensingServices
{
    public static class LicenseKeyGenerator
    {
        private const string CentralSecret = "POS_SYSTEM_v1_SECURE_KEY_2026_MASTER";

        public static string GenerateKey(int branchId, LicenseType type, int maxDevices, DateTime expiryDate)
        {
            var payload = new
            {
                BranchId = branchId,
                Type = type,
                MaxDevices = maxDevices,
                ExpiryDate = expiryDate,
                IssuedAt = DateTime.UtcNow
            };

            string json = JsonSerializer.Serialize(payload);
            byte[] encrypted = Encrypt(json, CentralSecret);
            return Convert.ToBase64String(encrypted);
        }

        public static (bool Success, int BranchId, LicenseType Type, int MaxDevices, DateTime ExpiryDate)? DecryptKey(string key)
        {
            try
            {
                byte[] encrypted = Convert.FromBase64String(key);
                string json = Decrypt(encrypted, CentralSecret);

                var payload = JsonSerializer.Deserialize<JsonElement>(json);
                return (
                    true,
                    payload.GetProperty("BranchId").GetInt32(),
                    (LicenseType)payload.GetProperty("Type").GetInt32(),
                    payload.GetProperty("MaxDevices").GetInt32(),
                    payload.GetProperty("ExpiryDate").GetDateTime()
                );
            }
            catch
            {
                return null; // Invalid key
            }
        }

        private static byte[] Encrypt(string text, string key)
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

        private static string Decrypt(byte[] data, string key)
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

        private static byte[] DeriveKey(string key)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        }
    }
}
