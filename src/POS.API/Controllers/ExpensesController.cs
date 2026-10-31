using Microsoft.AspNetCore.Mvc;
using POS.Contract.Dtos.ReportingDtos;
using POS.Services.ReportingServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace POS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExpenseDto>>> GetExpenses([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await _expenseService.GetExpensesAsync(from, to);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> AddExpense(ExpenseDto expense)
    {
        var result = await _expenseService.AddExpenseAsync(expense);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var result = await _expenseService.DeleteExpenseAsync(id);
        if (!result) return NotFound();
        return Ok();
    }
}
