namespace POS.Contract.Dtos.AppDateDtos;

public class AppDateToReturnDto
{
    public DateTime PosDate { get; set; }
    public DateTime StoreDate { get; set; }
    public int CurrentOrderNumber { get; set; }

}