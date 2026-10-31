namespace POS.Core.Entities.OrderEntity;

public enum OrderStates : byte
{
    Pending = 1,
    Canceled,
    Completed,
    Voided,
    Reserved,
    Dispatched,
    Delivered,
    FailedToDeliverToBranch,
    SentToBranch,
    Assigned,
    OnTable
}