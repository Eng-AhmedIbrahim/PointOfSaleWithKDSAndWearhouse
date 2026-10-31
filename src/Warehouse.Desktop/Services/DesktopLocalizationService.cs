using System.Globalization;
using Microsoft.Extensions.Localization;
using BlazorBase;


using BlazorBase.Services;
using Blazored.LocalStorage;

namespace Warehouse.Desktop.Services;

public class DesktopLocalizationService : LocalizationService
{
    public DesktopLocalizationService(
        IStringLocalizer<AppResources> localizer, 
        CommonProperties commonProperties,
        DesktopFileStorageWrapper localStorageWrapper) 
        : base(localizer, commonProperties, localStorageWrapper)
    {
    }
}

public class DesktopFileStorageWrapper : ILocalStorageService
{
    private readonly DesktopFileStorageService _fileStorage;

    public DesktopFileStorageWrapper(DesktopFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public event EventHandler<ChangingEventArgs>? Changing;
    public event EventHandler<ChangedEventArgs>? Changed;

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        => new(_fileStorage.ClearAsync());

    public ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_fileStorage.ContainsKey(key));

    public ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default)
        => new(_fileStorage.GetItemAsync<string>(key));

    public ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        => new(_fileStorage.GetItemAsync<T>(key));

    public ValueTask<string?> KeyAsync(int index, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<string?>(_fileStorage.KeyAt(index));

    public ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IEnumerable<string>>(_fileStorage.GetKeys());

    public ValueTask<int> LengthAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_fileStorage.Count);

    public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        => new(_fileStorage.RemoveItemAsync(key));

    public async ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        foreach (var key in keys)
            await _fileStorage.RemoveItemAsync(key);
    }

    public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
        => new(_fileStorage.SetItemAsync(key, data));

    public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        => new(_fileStorage.SetItemAsync(key, data));
}

