namespace BlazorBase.ERPFrontServices;

public class CustomizationSettingsService(IJSRuntime jsRuntime, ILocalStorageService localStorage)
    : ICustomizationSettingsService
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    public event Action? OnChanged;
    public string TableMaxHeight { get; private set; } = "65%";
    public string NotesMaxHeight { get; private set; } = "15%";

    public async Task LoadMaxHeights()
    {
        TableMaxHeight = await LoadHeightFromStorage(nameof(TableMaxHeight), "65%");
        NotesMaxHeight = await LoadHeightFromStorage(nameof(NotesMaxHeight), "15%");
    }

    private async Task<string> LoadHeightFromStorage(string key, string defaultValue)
        => await localStorage.GetItemAsync<string>(key) ?? defaultValue;

    public async Task AdjustHeight(string element, double adjustment)
    {
        switch (element)
        {
            case nameof(TableMaxHeight):
                TableMaxHeight = UpdateHeight(TableMaxHeight, adjustment);
                await SaveHeight(nameof(TableMaxHeight), TableMaxHeight);
                break;
            case nameof(NotesMaxHeight):
                NotesMaxHeight = UpdateHeight(NotesMaxHeight, adjustment);
                await SaveHeight(nameof(NotesMaxHeight), NotesMaxHeight);
                break;
        }

        NotifyStateChanged();
    }

    private string UpdateHeight(string height, double adjustment)
    {
        var trimmedHeight = height.TrimEnd('%');
        return double.TryParse(trimmedHeight, out var result)
            ? $"{result + adjustment}%"
            : height;
    }

    private async Task SaveHeight(string key, string height)
        => await localStorage.SetItemAsync(key, height);

    private void NotifyStateChanged() => OnChanged?.Invoke();

}