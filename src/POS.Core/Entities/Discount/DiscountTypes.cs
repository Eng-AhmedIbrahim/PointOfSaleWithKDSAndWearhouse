namespace POS.Core.Entities.Discount;

public enum DiscountTypes : byte
{
    Percentage = 1,
    FixedAmount,
    BuyOneGetOneFree,
    Seasonal,
    Promotional,
    Clearance,
    Loyalty,
    Referral,
    SpecialEvent,
    Other
}