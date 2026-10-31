using POS.Desktop.Services;

namespace POS.Desktop.Components.POSSettingsComponents;

public partial class Section3SettingsComponent
{
    [Inject] private DesktopFontSizeService FontSizeService { get; set; } = null!;
    
    private const string ScreenButtons = "screen-buttons";
    private const string SpecialKeypadButton = "special-keypad-button";
    private const string SpecialIconBtn = "special-icon-btn";

    private async Task IncreaseSize(string className)
    {
        var step = className switch
        {
            ScreenButtons => 1.5,
            SpecialKeypadButton => 2.0,
            SpecialIconBtn => 3.0,
            _ => 0.0
        };
        
        if (step > 0)
        {
            await FontSizeService.UpdateFontSize(className, step);
        }
    }

    private async Task DecreaseSize(string className)
    {
        var step = className switch
        {
            ScreenButtons => -1.5,
            SpecialKeypadButton => -2.0,
            SpecialIconBtn => -3.0,
            _ => 0.0
        };
        
        if (step < 0)
        {
            await FontSizeService.UpdateFontSize(className, step);
        }
    }

    private async Task Reset()
        => await FontSizeService.ClearSection3FontSizes();
}
