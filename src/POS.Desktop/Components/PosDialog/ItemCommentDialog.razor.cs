namespace POS.Desktop.Components.PosDialog;

public partial class ItemCommentDialog
{
    private string? UserComment { get; set; }
    private AttributeDto? _selectedComment;
    private List<AttributeDto> _existingComments = new();

    protected override void OnInitialized()
    {
        LoadComments();
    }

    private void LoadComments()
    {
        _existingComments = _commonProperties.TableItems?
            .FirstOrDefault(i => i.Id == _cartService.SelectedItem?.Id)?
            .Attributes?
            .Where(attr => attr.Id >= 5000) // Manual comments have IDs >= 5000
            .ToList() ?? new List<AttributeDto>();
    }

    private void SelectComment(AttributeDto attr)
    {
        if (_selectedComment == attr)
        {
            // Clicking the same comment again deselects it
            ClearSelection();
            return;
        }
        _selectedComment = attr;
        UserComment = attr.Name;
    }

    private void ClearSelection()
    {
        _selectedComment = null;
        UserComment = string.Empty;
    }

    private void AddNewComment()
    {
        if (string.IsNullOrWhiteSpace(UserComment)) return;

        var result = _cartService.AddItemComment(UserComment.Trim());
        if (string.IsNullOrEmpty(result))
        {
            _snackbar.Add("يجب اختيار صنف أولاً", Severity.Warning);
            return;
        }

        _snackbar.Add("تم إضافة التعليق", Severity.Success);
        UserComment = string.Empty;
        _cartService.NotifyStateChanged();
        LoadComments();
        StateHasChanged();
    }

    private void SaveEdit()
    {
        if (_selectedComment == null || string.IsNullOrWhiteSpace(UserComment)) return;

        var result = _cartService.EditItemComment(_selectedComment.Name!, UserComment.Trim());
        if (result == null)
        {
            _snackbar.Add("حدث خطأ أثناء التعديل", Severity.Warning);
            return;
        }

        _snackbar.Add("تم تعديل التعليق", Severity.Success);
        ClearSelection();
        _cartService.NotifyStateChanged();
        LoadComments();
        StateHasChanged();
    }

    private void DeleteComment(AttributeDto attr)
    {
        if (attr?.Name == null) return;

        var result = _cartService.DeleteItemComment(attr.Name);
        if (result == null)
        {
            _snackbar.Add("لم يتم العثور على التعليق", Severity.Warning);
            return;
        }

        if (_selectedComment == attr)
            ClearSelection();

        _snackbar.Add("تم حذف التعليق", Severity.Info);
        _cartService.NotifyStateChanged();
        LoadComments();
        StateHasChanged();
    }

    private void CloseDialog()
        => _commonProperties.ItemCommentDialogReference?.Close();
}
