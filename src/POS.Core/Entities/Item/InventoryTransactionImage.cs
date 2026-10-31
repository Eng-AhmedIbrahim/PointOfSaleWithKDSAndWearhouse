using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Core.Entities.Item;

public class InventoryTransactionImage : BaseEntity
{
    public int InventoryTransactionId { get; set; }
    
    [ForeignKey("InventoryTransactionId")]
    public InventoryTransaction? Transaction { get; set; }

    [Required]
    public string Base64Content { get; set; } = string.Empty;
}
