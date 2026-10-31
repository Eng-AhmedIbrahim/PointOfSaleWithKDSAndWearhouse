using System.Threading.Tasks;

namespace BlazorBase.ERPFrontServices.LicensingServices
{
    public interface ILicenseService
    {
        /// <summary>
        /// Checks if the device is currently licensed and activated.
        /// </summary>
        Task<bool> IsLicensedAsync();

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
        /// Checks if the BackOffice functionality is authorized (Requires LicenseType.Full).
        /// </summary>
        Task<bool> IsBackOfficeAuthorizedAsync();
    }
}
