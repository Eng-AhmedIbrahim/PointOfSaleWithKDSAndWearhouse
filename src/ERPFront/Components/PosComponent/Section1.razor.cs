namespace ERPFront.Components.PosComponent;

public partial class Section1
{

    [Parameter]
    public EventCallback<ICollection<MenuSalesItemsToReturnDto>> OnItemsFetched { get; set; }

    private ICollection<CategoryToReturnDto>? _categories = new List<CategoryToReturnDto>();
    private static readonly JsonSerializerOptions option = new() { PropertyNameCaseInsensitive = true };
    HttpClient? client = new();

    [Inject]
    private IHttpClientFactory? _clientFactory { get; set; }

    protected async override Task OnInitializedAsync()
    {
        client = _clientFactory?.CreateClient(ConstantStrings.ApiUrlName);
        var response = await client!.GetFromJsonAsync<List<CategoryToReturnDto>>(ConstantStrings.GetAllCategoriesUrl) ?? new();

        if (!response.Any())
            _categories = new List<CategoryToReturnDto>();

        _categories = response.ToList();
    }

    private async Task GetItemsByCatId(int catId)
    {
        string url = $"{ConstantStrings.GetItemsByCategoryId}?catId={catId}";

        var response = await client?.GetAsync(url) ?? new();
        var dataAsStringStream = await response.Content.ReadAsStringAsync();
        List<MenuSalesItemsToReturnDto>? items = JsonSerializer.Deserialize<List<MenuSalesItemsToReturnDto>>(dataAsStringStream
            , new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();


        await OnItemsFetched.InvokeAsync(items);
    }
}