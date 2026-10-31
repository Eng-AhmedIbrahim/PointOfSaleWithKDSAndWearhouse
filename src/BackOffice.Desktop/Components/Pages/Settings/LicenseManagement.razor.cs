using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using POS.Core.Entities.Settings;
using System.Net.Http.Headers;

namespace BackOffice.Desktop.Components.Pages.Settings
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
                Snackbar.Add($"حدث خطأ أثناء جلب التراخيص: {ex.Message}", Severity.Error);
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
                "تأكيد إزالة الارتباط",
                $"هل أنت متأكد من فك ارتباط الجهاز [{license.ComputerName}] بهذا الترخيص؟ سيُطلب من هذا الجهاز إدخال كود تفعيل جديد في المرة القادمة.",
                yesText: "نعم، فك الارتباط", cancelText: "إلغاء");

            if (result != true) return;

            try
            {
                var request = new { LicenseKey = license.LicenseKey, BranchId = license.BranchID };
                var response = await HttpClient.PostAsJsonAsync("api/License/UnlinkDevice", request);

                if (response.IsSuccessStatusCode)
                {
                    Snackbar.Add("تم فك الارتباط بنجاح. الترخيص أصبح متاحاً الآن للاستخدام في جهاز آخر.", Severity.Success);
                    await LoadLicensesAsync();
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Snackbar.Add($"حدث خطأ: {error}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"فشل الاتصال: {ex.Message}", Severity.Error);
            }
        }
    }
}
