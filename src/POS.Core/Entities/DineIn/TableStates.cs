namespace POS.Core.Entities.DineIn;

public enum TableState : byte
{
    Available = 0,
    OnTable,
    Reserved,
    Occupied,
    Closed
}