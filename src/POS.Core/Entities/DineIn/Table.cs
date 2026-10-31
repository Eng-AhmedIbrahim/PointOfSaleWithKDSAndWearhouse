using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Core.Entities.DineIn;

public class Table :BaseEntity
{
    public int BranchID { get; set; } = 1;
    public string? TableName { get; set; }
    public TableState TableState { get; set; } = TableState.Available;
    public int? SeatsCount { get; set; } 
    public int? LocationX { get; set; }
    public int? LocationY { get; set; }
    public bool? SmokingSection { get; set; }
    public bool? NearWindows { get; set; }
    public bool? BoothSeating { get; set; } 
    public bool? PrivateSeating { get; set; } 
    public int? LoadIndex { get; set; } 
    public bool? Usable { get; set; } 
    public string? TableShape { get; set; } 
    public string? TimeStamp { get; set; }
    public string? ImageUrl { get; set; }
    public int? GroupID { get; set; }
    
    [ForeignKey(nameof(GroupID))]
    public TableGroup? TableGroup { get; set; }
}