using System;
using System.Collections.Generic;

namespace POS.Core.Entities.Item;

public class Unit : BaseEntity
{
    public string? ArabicName { get; set; }
    public string? EnglishName { get; set; }
    public string? Code { get; set; } // e.g., kg, pcs, l
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
