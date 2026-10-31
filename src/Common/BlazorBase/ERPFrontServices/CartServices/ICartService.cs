using POS.Contract.Models;

namespace BlazorBase.ERPFrontServices.CartServices;
public interface ICartService
{
    public void SetSelectedItem(TableItem item);

    public void AppendNumberToQuantity(string number);
    public void OnClickBS();
    public void IncrementQuantity();
    public void DecrementQuantity();
    public void RemoveItem(List<TableItem> items);
    public void UpdateFinanceSettingsByMode(string posMode);

    public void ClearDineInOrderAttributes();

    public void ResetDiscount();
    public string? AddItemComment(string comment);
    public string? EditItemComment(string oldComment, string newComment);
    public string? DeleteItemComment(string comment);
    public void AddItemDiscount(string discountType, decimal discountValue, string? discountReason = null);
    public void ApplyOrderDiscount(decimal discountValue, bool isPercentage, DiscountReason reason);
   
    //public void AppendSpecialNumberToQuantity(string number);
}

