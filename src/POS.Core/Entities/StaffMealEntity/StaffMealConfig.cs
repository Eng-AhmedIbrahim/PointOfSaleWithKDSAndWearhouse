using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using POS.Core.Entities.Item;

namespace POS.Core.Entities.StaffMealEntity
{
    public class StaffMealConfig : BaseEntity
    {
        [Required]
        public string UserId { get; set; }
        
        public string? UserName { get; set; }

        public int? ItemId { get; set; }
        
        [ForeignKey(nameof(ItemId))]
        public virtual MenuSalesItems? Item { get; set; }

        public string? ItemName { get; set; }

        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        public int? GroupId { get; set; }
        public string? GroupName { get; set; }

        public int DailyLimit { get; set; } = 1;
        public decimal DailyAmountLimit { get; set; } = 0; // 0 means no amount limit, using count only
        public bool IsAllItemsAllowed { get; set; } = false;
        public int MealLimit { get; set; } = 1;
        
        public decimal SpecialPrice { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
    }

    public class StaffMealGroup : BaseEntity
    {
        [Required]
        public string Name { get; set; }
        public string? ArabicName { get; set; }
        public decimal DailyAmountLimit { get; set; } = 0;
        public bool IsAllItemsAllowed { get; set; } = false;
        public int DailyLimit { get; set; } = 1;
        public int MealLimit { get; set; } = 1;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<StaffMealGroupItem> GroupItems { get; set; } = new List<StaffMealGroupItem>();
    }

    public class StaffMealGroupItem : BaseEntity
    {
        [Required]
        public int GroupId { get; set; }
        
        [ForeignKey(nameof(GroupId))]
        public virtual StaffMealGroup Group { get; set; }

        [Required]
        public int ItemId { get; set; }
        
        [ForeignKey(nameof(ItemId))]
        public virtual MenuSalesItems Item { get; set; }
    }

    public class StaffMealUsage : BaseEntity
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public int ItemId { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public int? OrderId { get; set; }
    }
}
