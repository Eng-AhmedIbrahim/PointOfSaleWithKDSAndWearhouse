namespace POS.Core.Entities.Delivery;

public enum DeliveryStates : byte
{
    Pending = 1 ,
    OutForDelivery,
    Collected,
    Canceled
}
