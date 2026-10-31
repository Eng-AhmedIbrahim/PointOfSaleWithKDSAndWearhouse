using Microsoft.JSInterop;

namespace Warehouse.Desktop.Services;

public class DesktopFontSizeService
{
    private readonly DesktopFileStorageService _fileStorage;
    private readonly IJSRuntime _jsRuntime;

    public DesktopFontSizeService(DesktopFileStorageService fileStorage, IJSRuntime jsRuntime)
    {
        _fileStorage = fileStorage;
        _jsRuntime = jsRuntime;
    }

    public async Task UpdateFontSize(string className, double step)
    {
        var rootFontSize = 16.0; // Default root font size
        var cssVarName = GetCssVariableName(className);
        if (string.IsNullOrEmpty(cssVarName))
            return;

        // Get current size from storage or default
        var storageKey = $"fontSize_{className}";
        var currentSizeRem = await _fileStorage.GetItemAsync<double?>(storageKey) ?? 1.25;

        // Calculate new size
        var stepRem = step / rootFontSize;
        var newFontSizeRem = currentSizeRem + stepRem;

        // Update CSS variable via JS
        await _jsRuntime.InvokeVoidAsync("eval", 
            $"document.documentElement.style.setProperty('{cssVarName}', '{newFontSizeRem}rem')");

        // Save to file storage
        await _fileStorage.SetItemAsync(storageKey, newFontSizeRem);
    }

    public async Task LoadStoredFontSizes()
    {
        var fontSizes = new Dictionary<string, string>
        {
            { "special-keypad-button", "--dynamic-keypad-btn-size" },
            { "special-icon-btn", "--dynamic-icon-size" },
            { "special-quantity-button", "--dynamic-quantity-btn-size" },
            { "finance-label-text-size", "--dynamic-finance-label-text-size" },
            { "finance-input-text-size", "--dynamic-finance-input-text-size" },
            { "special-sec4-button", "--dynamic-sec4-btn-size" }
        };

        foreach (var (className, cssVar) in fontSizes)
        {
            var storageKey = $"fontSize_{className}";
            var size = await _fileStorage.GetItemAsync<double?>(storageKey);
            if (size.HasValue)
            {
                await _jsRuntime.InvokeVoidAsync("eval",
                    $"document.documentElement.style.setProperty('{cssVar}', '{size.Value}rem')");
            }
        }
    }

    public async Task ClearSection3FontSizes()
    {
        var keys = new[] 
        { 
            "fontSize_special-keypad-button",
            "fontSize_special-quantity-button",
            "fontSize_special-icon-btn"
        };

        foreach (var key in keys)
        {
            await _fileStorage.RemoveItemAsync(key);
        }

        // Reset CSS variables
        await _jsRuntime.InvokeVoidAsync("eval",
            "document.documentElement.style.setProperty('--dynamic-keypad-btn-size', '1rem');" +
            "document.documentElement.style.setProperty('--dynamic-icon-size', '1.25rem');" +
            "document.documentElement.style.setProperty('--dynamic-quantity-btn-size', '1rem');");
    }

    public async Task ClearSection4FontSizes()
    {
        var keys = new[]
        {
            "fontSize_finance-label-text-size",
            "fontSize_finance-input-text-size",
            "fontSize_special-sec4-button"
        };

        foreach (var key in keys)
        {
            await _fileStorage.RemoveItemAsync(key);
        }

        // Reset CSS variables
        await _jsRuntime.InvokeVoidAsync("eval",
            "document.documentElement.style.setProperty('--dynamic-finance-label-text-size', '1.125rem');" +
            "document.documentElement.style.setProperty('--dynamic-sec4-btn-size', '1.125rem');" +
            "document.documentElement.style.setProperty('--dynamic-finance-input-text-size', '1rem');");
    }

    private string GetCssVariableName(string className)
    {
        return className switch
        {
            "special-keypad-button" => "--dynamic-keypad-btn-size",
            "special-icon-btn" => "--dynamic-icon-size",
            "special-quantity-button" => "--dynamic-quantity-btn-size",
            "finance-label-text-size" => "--dynamic-finance-label-text-size",
            "finance-input-text-size" => "--dynamic-finance-input-text-size",
            "special-sec4-button" => "--dynamic-sec4-btn-size",
            _ => string.Empty
        };
    }
}

