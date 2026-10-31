using BlazorBase.API;
using POS.Contract.Dtos.CompanyDtos;
using Microsoft.Extensions.Logging;
using BlazorBase.Helpers;

namespace BlazorBase.ERPFrontServices.CompanyServices;

public class CompanyService : ICompanyService
{
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<CompanyService> _logger;
    private readonly HttpClient _httpClient;

    public CompanyService(ApiSettings apiSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<CompanyService> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(_apiSettings.ApiName!);
    }

    public async Task<IReadOnlyList<CreateCompanyDto>> GetCompanies()
    {
        var response = await _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllCompanies);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<List<CreateCompanyDto>>(content) ?? [];
        }
        return [];
    }

    public async Task<CreateCompanyDto?> GetCompanyById(int id)
    {
        var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetCompanyById}/{id}");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<CreateCompanyDto>(content);
        }
        return null;
    }

    public async Task<CreateCompanyDto?> CreateCompany(CreateCompanyDto companyDto)
    {
        var response = await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.CreateCompany, companyDto);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<CreateCompanyDto>(content);
        }
        return null;
    }

    public async Task<CreateCompanyDto?> UpdateCompany(UpdatedCompanyDto companyDto)
    {
        var response = await _httpClient.PutAsJsonAsync(_apiSettings.Endpoints!.UpdateCompany, companyDto);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<CreateCompanyDto>(content);
        }
        return null;
    }

    public async Task<bool> DeleteCompany(int id)
    {
        var response = await _httpClient.DeleteAsync($"{_apiSettings.Endpoints!.DeleteCompany}?companyId={id}");
        return response.IsSuccessStatusCode;
    }
}
