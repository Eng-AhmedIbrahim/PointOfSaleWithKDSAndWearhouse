namespace POS.Contract.Dtos.DineInDtos;

public class TableToReturnDto
{
    public int? Id { get; set; }
    public int BranchID { get; set; } = 1;
    public string? TableName { get; set; }
    public int? SeatsCount { get; set; }
    public int? GroupID { get; set; }
    public string? TableState { get; set; }

}