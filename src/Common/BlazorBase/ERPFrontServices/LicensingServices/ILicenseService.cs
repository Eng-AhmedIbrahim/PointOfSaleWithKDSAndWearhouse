using System.Threading.Tasks;

namespace BlazorBase.ERPFrontServices.LicensingServices
{
    public enum LicenseCheckStatus
    {
        Valid,
        Unlicensed,
        ApiUnreachable,
        DatabaseUnavailable
    }

    public class LicenseVerificationResult
    {
        public LicenseCheckStatus Status { get; set; }
        public bool IsLicensed => Status == LicenseCheckStatus.Valid;
        public bool IsConnectionError => Status == LicenseCheckStatus.ApiUnreachable || Status == LicenseCheckStatus.DatabaseUnavailable;
        public string? LicenseType { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface ILicenseService
    {
        /// <summary>
        /// Checks if the device is currently licensed and activated.
        /// </summary>
        Task<bool> IsLicensedAsync();

        /// <summary>
        /// Comprehensive check: tests API and DB connection first, then verifies license.
        /// Distinguishes between connection failure and missing license.
        /// </summary>
        Task<LicenseVerificationResult> VerifyDeviceLicenseAsync(string requiredScope = "POS");

        /// <summary>
        /// Checks if API and DB are reachable.
        /// </summary>
        Task<(bool IsConnected, string? ErrorMessage)> CheckHealthAsync();

        /// <summary>
        /// Gets the unique hardware identifier for the current machine.
        /// </summary>
        Task<string> GetHardwareIdAsync();

        /// <summary>
        /// Attempts to activate the software using admin credentials and a license key.
        /// </summary>
        Task<(bool Success, string Message)> ActivateAsync(ActivationRequest request);

        /// <summary>
        /// Verifies the administrative credentials without activating.
        /// </summary>
        bool VerifyAdminCredentials(string username, string password);

        /// <summary>
        /// Retrieves current license details if available.
        /// </summary>
        Task<LicenseInfo?> GetLicenseInfoAsync();

        /// <summary>
        /// Checks if the BackOffice functionality is authorized (Requires LicenseType.Full or BackOfficeOnly).
        /// </summary>
        Task<bool> IsBackOfficeAuthorizedAsync();

        /// <summary>
        /// Checks if the Warehouse functionality is authorized (Requires LicenseType.Full or WarehouseOnly).
        /// </summary>
        Task<bool> IsWarehouseAuthorizedAsync();
    }
}
