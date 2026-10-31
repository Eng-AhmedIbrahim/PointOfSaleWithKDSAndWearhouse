namespace ERPFront.Components.PosDialog;

public partial class ItemCommentDialog
{
    private string? UserComment { get; set; }

    private void SubmitComment()
    {

        var result = _cartService.AddItemComment(UserComment!);

        if (string.IsNullOrEmpty(result))
        {
            _snackbar.Add("Please Select Item First ", Severity.Warning);
            return;
        }

        _cartService.NotifyStateChanged();

        CloseDialog();

        return;
    }

    private void EditComment()
    {

        var oldComment = _commonProperties.TableItems!
       .FirstOrDefault(i => i.Id == _cartService.SelectedItem?.Id)?
       .Attributes?
       .LastOrDefault()?.Name;


        if (string.IsNullOrWhiteSpace(oldComment))
        {
            _snackbar.Add("Please select the comment to edit", Severity.Warning);
            return;
        }

        var result = _cartService.EditItemComment(oldComment!, UserComment!);

        if (string.IsNullOrEmpty(result))
        {
            _snackbar.Add("Please Select Item First or Comment not found", Severity.Warning);
            return;
        }

        _cartService.NotifyStateChanged();
        CloseDialog();
    }

    private void DeleteComment()
    {

        var oldComment = _commonProperties.TableItems!
       .FirstOrDefault(i => i.Id == _cartService.SelectedItem?.Id)?
       .Attributes?
       .LastOrDefault()?.Name;

        if (string.IsNullOrWhiteSpace(oldComment))
        {
            _snackbar.Add("Please enter the comment to delete", Severity.Warning);
            return;
        }

        var result = _cartService.DeleteItemComment(oldComment!);

        if (string.IsNullOrEmpty(result))
        {
            _snackbar.Add("Please Select Item First or Comment not found", Severity.Warning);
            return;
        }

        _cartService.NotifyStateChanged();
        CloseDialog();
    }

    private void CloseDialog()
        => _commonProperties.ItemCommentDialogReference?.Close();
}
