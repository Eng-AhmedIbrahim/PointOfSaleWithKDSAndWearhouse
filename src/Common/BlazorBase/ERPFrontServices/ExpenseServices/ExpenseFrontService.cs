using POS.Contract.Dtos.ReportingDtos;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System;

namespace BlazorBase.ERPFrontServices.ExpenseServices;

public class ExpenseFrontService : IExpenseFrontService
{
    private readonly HttpClient _httpClient;

    public ExpenseFrontService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ExpenseDto>> GetExpenses(DateTime from, DateTime to)
    {
        var url = $"api/expenses?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        var response = await _httpClient.GetFromJsonAsync<List<ExpenseDto>>(url);
        return response ?? new List<ExpenseDto>();
    }

    public async Task<ExpenseDto> AddExpense(ExpenseDto expense)
    {
        var response = await _httpClient.PostAsJsonAsync("api/expenses", expense);
        return await response.Content.ReadFromJsonAsync<ExpenseDto>() ?? new ExpenseDto();
    }

    public async Task<bool> DeleteExpense(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/expenses/{id}");
        return response.IsSuccessStatusCode;
    }
}
