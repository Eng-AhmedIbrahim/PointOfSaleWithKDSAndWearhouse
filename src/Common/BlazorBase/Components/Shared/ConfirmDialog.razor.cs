using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace BlazorBase.Components.Shared;

public partial class ConfirmDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public string? Title { get; set; }
    [Parameter] public string? ContentText { get; set; }
    [Parameter] public string? ConfirmText { get; set; }
    [Parameter] public string? CancelText { get; set; }
    [Parameter] public Color Color { get; set; } = Color.Primary;
    [Parameter] public string Icon { get; set; } = Icons.Material.Filled.Help;
    [Parameter] public string IconClass { get; set; } = "info";

    private void Submit() => MudDialog.Close(DialogResult.Ok(true));
    private void Cancel() => MudDialog.Cancel();
}
