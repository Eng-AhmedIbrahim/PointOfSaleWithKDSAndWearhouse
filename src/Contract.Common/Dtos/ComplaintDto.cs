namespace POS.Contract.Dtos;

public class ComplaintDto
{
    public int Id { get; set; }
    public string ComplaintNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public int? OrderId { get; set; }
    public int? OrderDatabaseId { get; set; }
    public string ComplaintText { get; set; } = string.Empty;
    public DateTime ComplaintDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Open";
    public string? Note { get; set; }
    public string? Resolution { get; set; }
    public DateTime? ResolutionDate { get; set; }
}
