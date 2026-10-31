using BlazorBase.API;
using POS.Contract.Dtos.CompanyDtos;
using Microsoft.Extensions.Logging;
using BlazorBase.Helpers;
using BlazorBase.ERPFrontServices.AppDateServices;

namespace BlazorBase.ERPFrontServices.BranchServices;

public class BranchService : IBranchService
{
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<AppDateService> _logger;
    private readonly HttpClient _httpClient;

    public BranchService(ApiSettings apiSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<AppDateService> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(_apiSettings.ApiName!);
    }

    public async Task<IReadOnlyList<BranchToReturnDto>> GetBranches()
    {
        var response = await _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllBranches);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<List<BranchToReturnDto>>(content) ?? [];
        }
        return [];
    }

    public async Task<BranchToReturnDto?> GetBranchById(int id)
    {
        var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetBranchById}/{id}");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<BranchToReturnDto>(content);
        }
        return null;
    }

    public async Task<BranchToReturnDto?> CreateBranch(BranchDto branchDto)
    {
        var response = await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.CreateBranch, branchDto);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<BranchToReturnDto>(content);
        }
        return null;
    }

    public async Task<BranchToReturnDto?> UpdateBranch(UpdatedBranchDto branchDto)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_apiSettings.Endpoints!.UpdateBranch}", branchDto);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<BranchToReturnDto>(content);
        }
        return null;
    }

    public async Task<bool> DeleteBranch(int id)
    {
        var response = await _httpClient.DeleteAsync($"{_apiSettings.Endpoints!.DeleteBranch}?branchId={id}");
        return response.IsSuccessStatusCode;
    }
}
