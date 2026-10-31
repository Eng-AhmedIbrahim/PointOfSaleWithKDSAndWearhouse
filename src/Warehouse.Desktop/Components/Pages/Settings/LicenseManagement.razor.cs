using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using POS.Core.Entities.Settings;
using System.Net.Http.Headers;

namespace Warehouse.Desktop.Components.Pages.Settings
{
    public partial class LicenseManagement : ComponentBase
    {
        [Inject] private HttpClient HttpClient { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;

        private List<License> _licenses = new();
        private bool _isLoading = false;
        private string _searchString = "";

        protected override async Task OnInitializedAsync()
        {
            await LoadLicensesAsync();
        }

        private async Task LoadLicensesAsync()
        {
            _isLoading = true;
            try
            {
                var response = await HttpClient.GetFromJsonAsync<List<License>>("api/License/GetAll");
                if (response != null)
                {
                    _licenses = response;
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø¬Ù„Ø¨ Ø§Ù„ØªØ±Ø§Ø®ÙŠØµ: {ex.Message}", Severity.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private bool FilterFunc(License license)
        {
            if (string.IsNullOrWhiteSpace(_searchString))
                return true;
            if (license.BranchName?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) == true)
                return true;
            if (license.LicenseKey?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) == true)
                return true;
            if (license.ComputerName?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) == true)
                return true;
            if (license.CustomerName?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) == true)
                return true;
            return false;
        }

        private async Task UnlinkDeviceAsync(License license)
        {
            bool? result = await DialogService.ShowMessageBox(
                "ØªØ£ÙƒÙŠØ¯ Ø¥Ø²Ø§Ù„Ø© Ø§Ù„Ø§Ø±ØªØ¨Ø§Ø·",
                $"Ù‡Ù„ Ø£Ù†Øª Ù…ØªØ£ÙƒØ¯ Ù…Ù† ÙÙƒ Ø§Ø±ØªØ¨Ø§Ø· Ø§Ù„Ø¬Ù‡Ø§Ø² [{license.ComputerName}] Ø¨Ù‡Ø°Ø§ Ø§Ù„ØªØ±Ø®ÙŠØµØŸ Ø³ÙŠÙØ·Ù„Ø¨ Ù…Ù† Ù‡Ø°Ø§ Ø§Ù„Ø¬Ù‡Ø§Ø² Ø¥Ø¯Ø®Ø§Ù„ ÙƒÙˆØ¯ ØªÙØ¹ÙŠÙ„ Ø¬Ø¯ÙŠØ¯ ÙÙŠ Ø§Ù„Ù…Ø±Ø© Ø§Ù„Ù‚Ø§Ø¯Ù…Ø©.",
                yesText: "Ù†Ø¹Ù…ØŒ ÙÙƒ Ø§Ù„Ø§Ø±ØªØ¨Ø§Ø·", cancelText: "Ø¥Ù„ØºØ§Ø¡");

            if (result != true) return;

            try
            {
                var request = new { LicenseKey = license.LicenseKey, BranchId = license.BranchID };
                var response = await HttpClient.PostAsJsonAsync("api/License/UnlinkDevice", request);

                if (response.IsSuccessStatusCode)
                {
                    Snackbar.Add("ØªÙ… ÙÙƒ Ø§Ù„Ø§Ø±ØªØ¨Ø§Ø· Ø¨Ù†Ø¬Ø§Ø­. Ø§Ù„ØªØ±Ø®ÙŠØµ Ø£ØµØ¨Ø­ Ù…ØªØ§Ø­Ø§Ù‹ Ø§Ù„Ø¢Ù† Ù„Ù„Ø§Ø³ØªØ®Ø¯Ø§Ù… ÙÙŠ Ø¬Ù‡Ø§Ø² Ø¢Ø®Ø±.", Severity.Success);
                    await LoadLicensesAsync();
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Snackbar.Add($"Ø­Ø¯Ø« Ø®Ø·Ø£: {error}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"ÙØ´Ù„ Ø§Ù„Ø§ØªØµØ§Ù„: {ex.Message}", Severity.Error);
            }
        }
    }
}

