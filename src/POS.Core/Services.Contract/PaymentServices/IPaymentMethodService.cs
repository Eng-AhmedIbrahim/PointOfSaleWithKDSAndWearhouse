/* file: POS.Core/Services.Contract/PaymentServices/IPaymentMethodService.cs */
using POS.Contract.Dtos.PaymentDtos;

namespace POS.Core.Services.Contract.PaymentServices;

public interface IPaymentMethodService
{
    Task<IEnumerable<PaymentMethodToReturnDto>> GetAllPaymentMethodsAsync();
    Task<PaymentMethodToReturnDto?> GetPaymentMethodByIdAsync(int id);
    Task<PaymentMethodToReturnDto> CreatePaymentMethodAsync(PaymentMethodDto paymentMethodDto);
    Task<bool> UpdatePaymentMethodAsync(int id, PaymentMethodDto paymentMethodDto);
    Task<bool> DeletePaymentMethodAsync(int id);
}
