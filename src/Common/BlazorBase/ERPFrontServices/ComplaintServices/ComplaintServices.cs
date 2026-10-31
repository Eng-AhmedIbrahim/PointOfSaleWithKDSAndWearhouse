using System.Net.Http.Json;
using BlazorBase.API;
using POS.Contract.Dtos;
using Microsoft.Extensions.Logging;

namespace BlazorBase.ERPFrontServices.ComplaintServices;

public class ComplaintServices : IComplaintServices
{
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<ComplaintServices> _logger;
    private readonly HttpClient _httpClient;

    public ComplaintServices(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<ComplaintServices> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    public async Task<ComplaintDto> CreateComplaintAsync(ComplaintDto complaintDto)
    {
        return await GetApiResponseAsync<ComplaintDto>(
            () => _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.CreateComplaint, complaintDto),
            "Failed to create complaint."
        ) ?? new();
    }

    public async Task<IReadOnlyList<ComplaintDto>> GetAllComplaintsAsync()
    {
        return await GetApiResponseAsync<List<ComplaintDto>>(
            () => _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllComplaints),
            "Failed to retrieve complaints."
        ) ?? new List<ComplaintDto>();
    }

    public async Task<ComplaintDto> GetComplaintByIdAsync(int id)
    {
        return await GetApiResponseAsync<ComplaintDto>(
            () => _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetComplaintById}/{id}"),
            "Failed to retrieve complaint."
        ) ?? new();
    }

    public async Task<IEnumerable<ComplaintDto>> GetComplaintsByPhoneAsync(string phone)
    {
        return await GetApiResponseAsync<List<ComplaintDto>>(
            () => _httpClient.GetAsync($"api/Complaint/phone/{phone}"),
            "Failed to retrieve complaints by phone."
        ) ?? new List<ComplaintDto>();
    }

    public async Task<bool> UpdateComplaintStatusAsync(int id, string status)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_apiSettings.Endpoints!.UpdateComplaintStatus}/{id}/status", status);
        return response.IsSuccessStatusCode;
    }

    private async Task<T?> GetApiResponseAsync<T>(
        Func<Task<HttpResponseMessage>> apiRequest,
        string? message)
    {
        var response = await ApiRequestHelpers.SendApiRequest(apiRequest);
        if (response is null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("API call failed: {ErrorMessage}", message ?? "No message provided.");
            return default;
        }

        var content = await response.Content.ReadAsStringAsync();
        var result = ApiRequestHelpers.DeserializeResponseContent<T>(content);

        return result;
    }
}
