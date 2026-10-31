namespace BlazorBase.ERPFrontServices;

public interface ICustomizationSettingsService
{
    event Action OnChanged;

    Task AdjustHeight(string element, double adjustment);

    Task LoadMaxHeights();

    string TableMaxHeight { get; }

    string NotesMaxHeight { get; }
}