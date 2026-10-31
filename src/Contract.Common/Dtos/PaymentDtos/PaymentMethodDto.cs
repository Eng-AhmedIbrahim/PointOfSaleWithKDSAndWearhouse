/* file: Contract.Common/Dtos/PaymentDtos/PaymentMethodDto.cs */
namespace POS.Contract.Dtos.PaymentDtos;

public class PaymentMethodDto
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class PaymentMethodToReturnDto : PaymentMethodDto
{
    public int Id { get; set; }
}
