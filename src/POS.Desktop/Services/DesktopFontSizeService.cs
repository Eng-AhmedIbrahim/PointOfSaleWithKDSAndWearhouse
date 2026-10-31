using Microsoft.JSInterop;

namespace POS.Desktop.Services;

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
        var storageKey = $"fontSize_{className}";
        var defaultSize = GetDefaultSizeRem(className);
        var currentSizeRem = await _fileStorage.GetItemAsync<double?>(storageKey) ?? defaultSize;

        // Calculate new size
        var stepRem = step / rootFontSize;
        var newFontSizeRem = Math.Clamp(currentSizeRem + stepRem, 0.6, 3.5);

        // Update CSS variables via JS
        if (className == "screen-buttons")
        {
            var catHeight = newFontSizeRem * 4.2;
            var catFont = newFontSizeRem * 1.05;
            await _jsRuntime.InvokeVoidAsync("eval",
                $"document.documentElement.style.setProperty('--dynamic-screen-btn-size', '{newFontSizeRem}rem');" +
                $"document.documentElement.style.setProperty('--dynamic-item-btn-size', '{newFontSizeRem}rem');" +
                $"document.documentElement.style.setProperty('--dynamic-category-btn-size', '{catFont}rem');" +
                $"document.documentElement.style.setProperty('--dynamic-category-btn-height', '{catHeight}rem');");
        }
        else
        {
            var cssVarName = GetCssVariableName(className);
            if (!string.IsNullOrEmpty(cssVarName))
            {
                await _jsRuntime.InvokeVoidAsync("eval", 
                    $"document.documentElement.style.setProperty('{cssVarName}', '{newFontSizeRem}rem')");
            }
        }

        // Save to file storage
        await _fileStorage.SetItemAsync(storageKey, newFontSizeRem);
    }

    public async Task LoadStoredFontSizes()
    {
        var fontSizes = new Dictionary<string, string>
        {
            { "screen-buttons", "--dynamic-screen-btn-size" },
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
                if (className == "screen-buttons")
                {
                    var catHeight = size.Value * 4.2;
                    var catFont = size.Value * 1.05;
                    await _jsRuntime.InvokeVoidAsync("eval",
                        $"document.documentElement.style.setProperty('--dynamic-screen-btn-size', '{size.Value}rem');" +
                        $"document.documentElement.style.setProperty('--dynamic-item-btn-size', '{size.Value}rem');" +
                        $"document.documentElement.style.setProperty('--dynamic-category-btn-size', '{catFont}rem');" +
                        $"document.documentElement.style.setProperty('--dynamic-category-btn-height', '{catHeight}rem');");
                }
                else
                {
                    await _jsRuntime.InvokeVoidAsync("eval",
                        $"document.documentElement.style.setProperty('{cssVar}', '{size.Value}rem')");
                }
            }
        }
    }

    public async Task ClearSection3FontSizes()
    {
        var keys = new[] 
        { 
            "fontSize_screen-buttons",
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
            "document.documentElement.style.setProperty('--dynamic-screen-btn-size', '1rem');" +
            "document.documentElement.style.setProperty('--dynamic-item-btn-size', '1rem');" +
            "document.documentElement.style.setProperty('--dynamic-category-btn-size', '1.05rem');" +
            "document.documentElement.style.setProperty('--dynamic-category-btn-height', '4.2rem');" +
            "document.documentElement.style.setProperty('--dynamic-keypad-btn-size', '1.4rem');" +
            "document.documentElement.style.setProperty('--dynamic-icon-size', '2rem');" +
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
            "document.documentElement.style.setProperty('--dynamic-finance-label-text-size', '0.95rem');" +
            "document.documentElement.style.setProperty('--dynamic-sec4-btn-size', '0.95rem');" +
            "document.documentElement.style.setProperty('--dynamic-finance-input-text-size', '1.15rem');");
    }

    public double GetDefaultSizeRem(string className)
    {
        return className switch
        {
            "screen-buttons" => 1.0,
            "special-keypad-button" => 1.4,
            "special-icon-btn" => 2.0,
            "special-quantity-button" => 1.0,
            "finance-label-text-size" => 0.95,
            "finance-input-text-size" => 1.15,
            "special-sec4-button" => 0.95,
            _ => 1.0
        };
    }

    private string GetCssVariableName(string className)
    {
        return className switch
        {
            "screen-buttons" => "--dynamic-screen-btn-size",
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
