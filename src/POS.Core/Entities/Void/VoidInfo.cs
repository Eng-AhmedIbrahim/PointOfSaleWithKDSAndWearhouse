namespace POS.Core.Entities.Void;

public class VoidInfo
{
    public decimal? VoidAmount { get; set; }
    public string? VoidBy { get; set; }
    public string? VoidByName { get; set; }
    public DateTime? VoidTime { get; set; }
    public string? VoidReason { get; set; }
    public int? VoidCount { get; set; }
    public decimal? TotalVoid { get; set; }
}