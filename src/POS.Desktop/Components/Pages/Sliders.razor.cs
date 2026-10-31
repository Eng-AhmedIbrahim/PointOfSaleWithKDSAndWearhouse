using System.Net.Http;

namespace POS.Desktop.Components.Pages
{
    public partial class Sliders
    {
        private ICollection<CategoryToReturnDto>? _categories = new List<CategoryToReturnDto>();
        private double _salesItemsHorizontalSider = 4.0;
        private double _salesItemsVerticalSlider = 4.0;
        private ICollection<MenuSalesItemsToReturnDto> _itemByCatId = new List<MenuSalesItemsToReturnDto>();
        HttpClient? client;
        string url = $"{ConstantStrings.GetItemsByCategoryId}?catId=1";

        protected async override Task OnInitializedAsync()
        {
            client = _clientFactory.CreateClient(ConstantStrings.ApiUrlName);
            var response = await client.GetAsync(ConstantStrings.GetAllCategoriesUrl);
            if (response.IsSuccessStatusCode)
            {
                var dataAsStringStream = await response.Content.ReadAsStringAsync();
                _categories = JsonSerializer.Deserialize<List<CategoryToReturnDto>>(dataAsStringStream
                    , new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); ;
            }

            var response2 = await client!.GetAsync(url) ?? new();
            if (response.IsSuccessStatusCode)
            {
                var dataAsStringStream = await response2.Content.ReadAsStringAsync();
                _itemByCatId = JsonSerializer.Deserialize<List<MenuSalesItemsToReturnDto>>(dataAsStringStream
                    , new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            await base.OnInitializedAsync();
        }
        private async Task SaveChanges()
        {
            // Implementation for saving changes
            await SafeNavigateAsync("/pos");
        }

        private async Task Cancel()
        {
            await SafeNavigateAsync("/pos");
        }

        private async Task SafeNavigateAsync(string uri)
        {
            int maxRetries = 5;
            int currentRetry = 0;
            int delayMs = 50;

            while (currentRetry < maxRetries)
            {
                try
                {
                    await Task.Delay(delayMs);
                    if (_navigationManager != null && !string.IsNullOrEmpty(_navigationManager.Uri))
                    {
                        await InvokeAsync(() => _navigationManager.NavigateTo(uri, forceLoad: false));
                        return;
                    }
                    else
                    {
                        throw new InvalidOperationException("NavigationManager not yet initialized");
                    }
                }
                catch (Exception ex)
                {
                    currentRetry++;
                    if (currentRetry >= maxRetries)
                    {
                        var logger = _clientFactory.CreateClient("POS.Desktop"); 
                        // Assuming basic logging or alert
                        // No snackbar defined in this file, so we just swallow or console log
                        Console.WriteLine($"Navigation failed: {ex.Message}");
                        return;
                    }
                    delayMs *= 2;
                }
            }
        }
    }
}
