/* file: POS.Core/Entities/Payment/PaymentMethodEntity.cs */
using System.ComponentModel.DataAnnotations;

namespace POS.Core.Entities.Payment;

public class PaymentMethodEntity : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NameEn { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
