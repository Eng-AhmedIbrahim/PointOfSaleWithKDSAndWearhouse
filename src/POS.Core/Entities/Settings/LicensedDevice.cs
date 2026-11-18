using System.ComponentModel.DataAnnotations;

namespace POS.Core.Entities.Settings
{
    public class LicensedDevice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string HardwareId { get; set; } = string.Empty;

        [Required]
        public int BranchId { get; set; }

        public Branch? Branch { get; set; }

        public DateTime ActivationDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string LicenseType { get; set; } = string.Empty; // e.g. "POSOnly", "Full"

        /// <summary>
        /// The license key this device was registered with.
        /// Used to scope maxDevices quota per key (not per branch-type).
        /// </summary>
        [MaxLength(500)]
        public string? LicenseKey { get; set; }

        /// <summary>
        /// HMAC-SHA256 Digital Signature verifying that HardwareId, LicenseKey, and ExpiryDate have not been tampered with in DB.
        /// </summary>
        [MaxLength(128)]
        public string? Signature { get; set; }
    }
}
