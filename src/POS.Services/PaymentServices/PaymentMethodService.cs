/* file: POS.Services/PaymentServices/PaymentMethodService.cs */
using POS.Contract.Dtos.PaymentDtos;
using POS.Core.Entities.Payment;
using POS.Core.Services.Contract.PaymentServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POS.Services.PaymentServices;

public class PaymentMethodService : IPaymentMethodService
{
    private readonly IUnitOfWork _unitOfWork;

    public PaymentMethodService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<PaymentMethodToReturnDto>> GetAllPaymentMethodsAsync()
    {
        var methods = await _unitOfWork.Repository<PaymentMethodEntity>().GetAllAsync();
        return methods.Select(m => new PaymentMethodToReturnDto
        {
            Id = m.Id,
            NameAr = m.NameAr,
            NameEn = m.NameEn,
            IsActive = m.IsActive
        });
    }

    public async Task<PaymentMethodToReturnDto?> GetPaymentMethodByIdAsync(int id)
    {
        var m = await _unitOfWork.Repository<PaymentMethodEntity>().GetByIdAsync(id);
        if (m == null) return null;

        return new PaymentMethodToReturnDto
        {
            Id = m.Id,
            NameAr = m.NameAr,
            NameEn = m.NameEn,
            IsActive = m.IsActive
        };
    }

    public async Task<PaymentMethodToReturnDto> CreatePaymentMethodAsync(PaymentMethodDto paymentMethodDto)
    {
        var method = new PaymentMethodEntity
        {
            NameAr = paymentMethodDto.NameAr,
            NameEn = paymentMethodDto.NameEn,
            IsActive = paymentMethodDto.IsActive
        };

        await _unitOfWork.Repository<PaymentMethodEntity>().AddAsync(method);
        await _unitOfWork.CompleteAsync();

        return new PaymentMethodToReturnDto
        {
            Id = method.Id,
            NameAr = method.NameAr,
            NameEn = method.NameEn,
            IsActive = method.IsActive
        };
    }

    public async Task<bool> UpdatePaymentMethodAsync(int id, PaymentMethodDto paymentMethodDto)
    {
        var method = await _unitOfWork.Repository<PaymentMethodEntity>().GetByIdAsync(id);
        if (method == null) return false;

        method.NameAr = paymentMethodDto.NameAr;
        method.NameEn = paymentMethodDto.NameEn;
        method.IsActive = paymentMethodDto.IsActive;

        _unitOfWork.Repository<PaymentMethodEntity>().Update(method);
        await _unitOfWork.CompleteAsync();
        return true;
    }

    public async Task<bool> DeletePaymentMethodAsync(int id)
    {
        var method = await _unitOfWork.Repository<PaymentMethodEntity>().GetByIdAsync(id);
        if (method == null) return false;

        _unitOfWork.Repository<PaymentMethodEntity>().Delete(method);
        await _unitOfWork.CompleteAsync();
        return true;
    }
}
