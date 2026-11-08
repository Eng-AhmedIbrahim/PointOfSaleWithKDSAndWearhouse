using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace POS.API.Helpers;

public static class EncryptionHelper
{
    private static readonly string Key = "b14ca5898a4e4133bbce2ea2315a1916"; // 32 chars
    
    public static string DecryptString(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        // Check if string is already plain (simple heuristic: starts with "Server=" or doesn't look like Base64)
        if (cipherText.StartsWith("Server=") || !IsBase64String(cipherText))
        {
            return cipherText;
        }

        try
        {
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Key);
                aes.IV = new byte[16]; // Zero IV matching encryption
                
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
        catch
        {
            // Fallback to original if decryption fails
            return cipherText;
        }
    }

    public static string EncryptString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        try
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Key);
                aes.IV = new byte[16]; // نفس الـ IV الأصفار المستخدم في فك التشفير

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                    }
                    // تحويل النتيجة النهائية لـ Base64 عشان تعرف تخزنها كـ String
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        catch
        {
            // في حالة الخطأ رجع النص الأصلي أو ارمي Exception حسب حاجتك
            return plainText;
        }
    }

    private static bool IsBase64String(string s)
    {
        Span<byte> buffer = new Span<byte>(new byte[s.Length]);
        return Convert.TryFromBase64String(s, buffer, out _);
    }
}
