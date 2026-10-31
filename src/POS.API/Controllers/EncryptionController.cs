using Microsoft.AspNetCore.Mvc;
using POS.API.Helpers;

namespace POS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EncryptionController : ControllerBase
{
    /// <summary>
    /// Encrypts a plain text string using the system's encryption key.
    /// Use this to generate encrypted connection strings for appsettings.json
    /// </summary>
    /// <param name="plainText">The string to encrypt (e.g., Server=.;Database=...)</param>
    /// <returns>Base64 encoded encrypted string</returns>
    [HttpGet("encrypt")]
    public ActionResult<string> Encrypt([FromQuery] string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return BadRequest("Text cannot be empty");

        var encrypted = EncryptionHelper.EncryptString(plainText);
        return Ok(encrypted);
    }

    /// <summary>
    /// Decrypts a Base64 encoded cipher text.
    /// </summary>
    /// <param name="cipherText">The Base64 string to decrypt</param>
    /// <returns>Plain text string</returns>
    [HttpGet("decrypt")]
    public ActionResult<string> Decrypt([FromQuery] string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return BadRequest("Cipher text cannot be empty");

        var decrypted = EncryptionHelper.DecryptString(cipherText);
        return Ok(decrypted);
    }
}
