using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Core.Entities.DineIn;

[NotMapped]
public class TableInfo
{
    public int? TableID { get; set; }
    public int? TableName { get; set; }
    public TableState TableState { get; set; }
}