/* file: Common/BlazorBase/ERPFrontServices/PaymentMethodServices/IPaymentMethodServices.cs */
using POS.Contract;
using POS.Contract.Dtos.PaymentDtos;

namespace BlazorBase.ERPFrontServices.PaymentMethodServices;

public interface IPaymentMethodServices
{
    Task<ICollection<PaymentMethodToReturnDto>> GetAllPaymentMethodsAsync();
    Task<ServiceResponse<PaymentMethodToReturnDto>> CreatePaymentMethod(PaymentMethodDto paymentMethod);
    Task<ServiceResponse<bool>> UpdatePaymentMethod(int id, PaymentMethodDto paymentMethod);
    Task<ServiceResponse<bool>> DeletePaymentMethod(int id);
}
