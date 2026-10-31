namespace ERPFront.Components.POSLayoutComponents;

public partial class SettingsDrawer
{
    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter] public Anchor Anchor { get; set; } = Anchor.Start;
    [Parameter] public bool OverlayAutoClose { get; set; } = true;
    [Parameter] public string Title { get; set; } = "Settings";
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string _buttonText = "Start";

    private async Task SetAnchor(Anchor anchor)
    {
        Anchor = anchor;
        _buttonText = anchor.ToString();
        await OpenChanged.InvokeAsync(Open);
    }

    private async Task CloseDrawer()
    {
        Open = false;
        await OpenChanged.InvokeAsync(Open);
    }

    private string CurrentPageName => GetCurrentPageName();

    private string GetCurrentPageName()
    {
        var currentRoute = Navigation.Uri.Replace(Navigation.BaseUri, "")
            .Trim('/');

        return currentRoute.ToLower() switch
        {
            "" or "index" => PagesNames.Home,
            "login" => PagesNames.Login,
            "register" => PagesNames.Register,
            "pos" => PagesNames.POS,
            _ => "Unknown" // Default for unmapped routes
        };
    }
}