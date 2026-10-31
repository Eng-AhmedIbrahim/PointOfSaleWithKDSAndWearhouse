using POS.Contract.Dtos.ReportingDtos;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace BlazorBase.ERPFrontServices.ExpenseServices;

public interface IExpenseFrontService
{
    Task<List<ExpenseDto>> GetExpenses(DateTime from, DateTime to);
    Task<ExpenseDto> AddExpense(ExpenseDto expense);
    Task<bool> DeleteExpense(int id);
}
