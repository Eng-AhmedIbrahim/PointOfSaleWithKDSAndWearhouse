using POS.Contract.Dtos.ReportingDtos;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using POS.Core.Repository.Contract;
using AutoMapper;
using POS.Core.Specifications;

namespace POS.Services.ReportingServices;

public interface IExpenseService
{
    Task<List<ExpenseDto>> GetExpensesAsync(DateTime from, DateTime to);
    Task<ExpenseDto> AddExpenseAsync(ExpenseDto expense);
    Task<bool> DeleteExpenseAsync(int id);
}

public class ExpenseService : IExpenseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ExpenseService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<ExpenseDto>> GetExpensesAsync(DateTime from, DateTime to)
    {
        var nextDay = to.Date.AddDays(1);
        var spec = new BaseSpecifications<POS.Core.Entities.Payment.Expense>(e => e.Date >= from.Date && e.Date < nextDay);
        var expenses = await _unitOfWork.Repository<POS.Core.Entities.Payment.Expense>().GetAllWithSpecificationAsync(spec);
        return _mapper.Map<List<ExpenseDto>>(expenses);
    }

    public async Task<ExpenseDto> AddExpenseAsync(ExpenseDto expenseDto)
    {
        var expense = new POS.Core.Entities.Payment.Expense
        {
            Date = expenseDto.Date == default ? DateTime.Now : expenseDto.Date,
            Amount = expenseDto.Amount,
            Description = expenseDto.Description,
            Category = expenseDto.Category,
            CreatedById = expenseDto.CreatedById,
            CreatedByName = expenseDto.CreatedByName,
            SpentBy = expenseDto.SpentBy,
            IsPayoutFromDrawer = expenseDto.IsPayoutFromDrawer,
            BranchId = expenseDto.BranchId,
            ShiftId = expenseDto.ShiftId
        };

        await _unitOfWork.Repository<POS.Core.Entities.Payment.Expense>().AddAsync(expense);
        await _unitOfWork.CompleteAsync();
        
        expenseDto.Id = expense.Id;
        return expenseDto;
    }

    public async Task<bool> DeleteExpenseAsync(int id)
    {
        var expense = await _unitOfWork.Repository<POS.Core.Entities.Payment.Expense>().GetByIdAsync(id);
        if (expense == null) return false;
        
        _unitOfWork.Repository<POS.Core.Entities.Payment.Expense>().Delete(expense);
        return await _unitOfWork.CompleteAsync() > 0;
    }
}
