using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace POS.Core.Helpers;

public static class EncryptionHelper
{
    private static readonly string Key = "b14ca5898a4e4133bbce2ea2315a1916"; // 32 chars

    public static string DecryptString(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

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
                aes.IV = new byte[16];

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
            return cipherText;
        }
    }

    private static bool IsBase64String(string base64)
    {
        Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
        return Convert.TryFromBase64String(base64, buffer, out _);
    }
}
