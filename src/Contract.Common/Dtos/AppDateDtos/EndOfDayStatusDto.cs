namespace POS.Contract.Dtos.AppDateDtos;

public class EndOfDayStatusDto
{
    public DateTime PosDate { get; set; }
    public bool HasOpenShifts { get; set; }
    public int OpenOrdersCount { get; set; }
    public bool CanCloseDay => !HasOpenShifts && OpenOrdersCount == 0;
    public string Message { get; set; } = string.Empty;
}
