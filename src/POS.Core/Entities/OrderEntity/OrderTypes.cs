namespace POS.Core.Entities.OrderEntity;

public enum OrderTypes : byte
{
    TakeAway = 1,
    Delivery,
    DineIn,
    Staff,
    Hospitality
}
