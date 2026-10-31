using System.Globalization;
using Microsoft.Extensions.Localization;



namespace BlazorBase.Services;

public class LocalizationService
{
    private readonly IStringLocalizer<AppResources> _localizer;
    private readonly CommonProperties _commonProperties;
    private readonly ILocalStorageService _localStorage;

    public event Action? OnLanguageChanged;

    public LocalizationService(IStringLocalizer<AppResources> localizer, CommonProperties commonProperties, ILocalStorageService localStorage)
    {
        _localizer = localizer;
        _commonProperties = commonProperties;
        _localStorage = localStorage;
    }

    public string this[string key] 
    {
        get
        {
            var culture = new CultureInfo(_commonProperties.Language ?? "ar");
            var translated = AppResources.ResourceManager.GetString(key, culture);
            return translated ?? _localizer[key];
        }
    }

    public async Task SetLanguage(string culture)
    {
        var cultureInfo = new CultureInfo(culture);
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;
        
        _commonProperties.Language = culture;
        
        await _localStorage.SetItemAsync("culture", culture);

        OnLanguageChanged?.Invoke();
    }

    public async Task InitializeAsync()
    {
        var culture = "ar";
        _commonProperties.IsRtl = false;
        
        await SetLanguage(culture);
    }
    
    public string GetCurrentLanguage() => _commonProperties.Language;
    
    public bool IsRtl() => _commonProperties.IsRtl;
}