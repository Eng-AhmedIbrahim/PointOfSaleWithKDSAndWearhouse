namespace ERPFront.Components.Pages
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

        }
    }
}
