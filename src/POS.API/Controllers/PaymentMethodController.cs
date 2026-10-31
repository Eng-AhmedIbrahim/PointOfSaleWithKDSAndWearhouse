/* file: POS.API/Controllers/PaymentMethodController.cs */
using Microsoft.AspNetCore.Mvc;
using POS.Contract.Dtos.PaymentDtos;
using POS.Core.Services.Contract.PaymentServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace POS.API.Controllers;

public class PaymentMethodController : BaseApiController
{
    private readonly IPaymentMethodService _paymentMethodService;

    public PaymentMethodController(IPaymentMethodService paymentMethodService)
    {
        _paymentMethodService = paymentMethodService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentMethodToReturnDto>>> GetPaymentMethods()
    {
        var result = await _paymentMethodService.GetAllPaymentMethodsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PaymentMethodToReturnDto>> GetPaymentMethod(int id)
    {
        var result = await _paymentMethodService.GetPaymentMethodByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentMethodToReturnDto>> CreatePaymentMethod(PaymentMethodDto paymentMethodDto)
    {
        var result = await _paymentMethodService.CreatePaymentMethodAsync(paymentMethodDto);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdatePaymentMethod(int id, PaymentMethodDto paymentMethodDto)
    {
        var result = await _paymentMethodService.UpdatePaymentMethodAsync(id, paymentMethodDto);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePaymentMethod(int id)
    {
        var result = await _paymentMethodService.DeletePaymentMethodAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
