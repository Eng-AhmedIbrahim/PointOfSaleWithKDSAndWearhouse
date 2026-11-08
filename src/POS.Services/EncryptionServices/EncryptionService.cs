using Microsoft.Extensions.Configuration;
using POS.Core.Services.Contract.EncryptionServices;
using System.Security.Cryptography;
using System.Text;

namespace POS.Services.EncryptionServices;

/// <summary>
/// AES-256-CBC encryption service with a random IV per operation.
/// Format stored in DB: Base64( IV(16 bytes) + CipherText )
/// This ensures the same plaintext produces a different ciphertext every time.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private const int IvSize = 16; // 128-bit IV

    public EncryptionService(IConfiguration configuration)
    {
        var keyString = configuration["EncryptionKey"]
            ?? throw new InvalidOperationException("EncryptionKey is not configured in appsettings.");

        // Ensure exactly 32 bytes for AES-256
        _key = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));
    }

    /// <summary>
    /// Encrypts plain text using AES-256-CBC with a fresh random IV each call.
    /// Returns: Base64(IV + CipherText)
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV(); // Fresh random IV every time → same input ≠ same output

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream();

        // Prepend the IV so it's available during decryption
        ms.Write(aes.IV, 0, IvSize);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Decrypts a Base64-encoded string that was encrypted with Encrypt().
    /// Falls back gracefully for plain-text legacy data.
    /// </summary>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        // Graceful fallback: if not valid Base64, it's unencrypted legacy data
        if (!IsBase64String(cipherText)) return cipherText;

        try
        {
            var fullBuffer = Convert.FromBase64String(cipherText);

            // Must have at least IV (16 bytes) + 1 block (16 bytes)
            if (fullBuffer.Length <= IvSize) return cipherText;

            // Extract IV from the first 16 bytes
            var iv = new byte[IvSize];
            Buffer.BlockCopy(fullBuffer, 0, iv, 0, IvSize);

            // Rest is the actual ciphertext
            var cipherBytes = new byte[fullBuffer.Length - IvSize];
            Buffer.BlockCopy(fullBuffer, IvSize, cipherBytes, 0, cipherBytes.Length);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);

            return sr.ReadToEnd();
        }
        catch
        {
            // Fallback: return as-is (e.g. old plain-text data in DB)
            return cipherText;
        }
    }

    private static bool IsBase64String(string s)
    {
        if (string.IsNullOrWhiteSpace(s) || s.Length % 4 != 0) return false;
        Span<byte> buffer = new byte[s.Length];
        return Convert.TryFromBase64String(s, buffer, out _);
    }
}
