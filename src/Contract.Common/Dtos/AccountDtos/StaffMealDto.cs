using System;
using System.Collections.Generic;

namespace POS.Contract.Dtos.AccountDtos
{
    public class StaffMealConfigDto
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? ItemId { get; set; }
        public string? ItemName { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? GroupId { get; set; }
        public string? GroupName { get; set; }
        public int DailyLimit { get; set; } // Max meals per day
        public decimal DailyAmountLimit { get; set; } = 0; // Max spent per day
        public bool IsAllItemsAllowed { get; set; } = false; // If true, can pick anything from menu
        public int MealLimit { get; set; } = 1; // Limit per single order
        public decimal SpecialPrice { get; set; } = 0; // Usually 0 for staff
        public bool IsActive { get; set; } = true;
    }

    public class StaffMealGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? ArabicName { get; set; }
        public decimal DailyAmountLimit { get; set; } = 0;
        public bool IsAllItemsAllowed { get; set; } = false;
        public int DailyLimit { get; set; } = 1;
        public int MealLimit { get; set; } = 1;
        public List<StaffMealGroupItemDto> Items { get; set; } = new();
    }

    public class StaffMealGroupItemDto
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public int CategoryId { get; set; }
    }

    public class StaffMealUsageDto
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? ItemId { get; set; }
        public DateTime UsageDate { get; set; }
        public string? OrderId { get; set; }
    }

    public class StaffMealStatusDto
    {
        public bool IsEligible { get; set; }
        public int RemainingToday { get; set; }
        public decimal RemainingAmountToday { get; set; }
        public StaffMealConfigDto? Config { get; set; }
    }
}
