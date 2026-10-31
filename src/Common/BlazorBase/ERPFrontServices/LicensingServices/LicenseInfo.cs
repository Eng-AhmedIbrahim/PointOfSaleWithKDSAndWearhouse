using System;

namespace BlazorBase.ERPFrontServices.LicensingServices
{
    public enum LicenseType
    {
        POSOnly = 1,
        BackOfficeOnly = 2,
        Full = 3
    }

    public class LicenseInfo
    {
        public string HardwareId { get; set; } = string.Empty;
        public string LicenseKey { get; set; } = string.Empty;
        public LicenseType Type { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int BranchId { get; set; }
        public int MaxDeviceCount { get; set; }
        public string Hmac { get; set; } = string.Empty;
    }

    public class ActivationRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string LicenseKey { get; set; } = string.Empty;
        public LicenseType Type { get; set; }
        public int BranchId { get; set; }
    }
}
