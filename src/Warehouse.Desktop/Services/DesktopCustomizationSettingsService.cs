using BlazorBase.ERPFrontServices;

namespace Warehouse.Desktop.Services;

public class DesktopCustomizationSettingsService : ICustomizationSettingsService
{
    private readonly DesktopFileStorageService _fileStorage;
    public event Action? OnChanged;
    public string TableMaxHeight { get; private set; } = "65%";
    public string NotesMaxHeight { get; private set; } = "15%";

    public DesktopCustomizationSettingsService(DesktopFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public async Task LoadMaxHeights()
    {
        TableMaxHeight = await LoadHeightFromStorage(nameof(TableMaxHeight), "65%");
        NotesMaxHeight = await LoadHeightFromStorage(nameof(NotesMaxHeight), "15%");
    }

    private async Task<string> LoadHeightFromStorage(string key, string defaultValue)
    {
        var value = await _fileStorage.GetItemAsync<string>(key);
        return value ?? defaultValue;
    }

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
    {
        await _fileStorage.SetItemAsync(key, height);
    }

    private void NotifyStateChanged() => OnChanged?.Invoke();
}

