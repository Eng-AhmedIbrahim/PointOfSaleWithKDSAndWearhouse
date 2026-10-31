/* file: Common/BlazorBase/ERPFrontServices/PaymentMethodServices/PaymentMethodServices.cs */
using BlazorBase.API;
using BlazorBase.Helpers;
using BlazorBase.Services;
using Microsoft.Extensions.Logging;
using POS.Contract;
using POS.Contract.Dtos.PaymentDtos;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BlazorBase.ERPFrontServices.PaymentMethodServices;

public class PaymentMethodServices : IPaymentMethodServices
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<PaymentMethodServices> _logger;

    public PaymentMethodServices(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<PaymentMethodServices> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(_apiSettings!.ApiName!);
    }

    public async Task<ICollection<PaymentMethodToReturnDto>> GetAllPaymentMethodsAsync()
    {
        try
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.GetAsync(_apiSettings.Endpoints!.GetPaymentMethods));
            if (response is null || !response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to retrieve payment methods.");
                return [];
            }

            var content = await response.Content.ReadAsStringAsync();
            var items = ApiRequestHelpers.DeserializeResponseContent<List<PaymentMethodToReturnDto>>(content);
            return items ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting payment methods.");
            return [];
        }
    }

    public async Task<ServiceResponse<PaymentMethodToReturnDto>> CreatePaymentMethod(PaymentMethodDto paymentMethod)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.CreatePaymentMethod, paymentMethod));
            if (response is null) return ServiceResponseHelpers.Failure<PaymentMethodToReturnDto>("Failed to connect to the API");
            
            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Payment Method created successfully");
            var result = response.IsSuccessStatusCode ? ApiRequestHelpers.DeserializeResponseContent<PaymentMethodToReturnDto>(await response.Content.ReadAsStringAsync()) : default;
            
            return result is null ? ServiceResponseHelpers.Failure<PaymentMethodToReturnDto>(responseMessage) : ServiceResponseHelpers.Success(result, responseMessage);
        }, "Failed to Create Payment Method");
    }

    public async Task<ServiceResponse<bool>> UpdatePaymentMethod(int id, PaymentMethodDto paymentMethod)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PutAsJsonAsync($"{_apiSettings.Endpoints!.UpdatePaymentMethod}/{id}", paymentMethod));
            if (response is null) return ServiceResponseHelpers.Failure<bool>("Failed to connect to the API");
            
            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Payment Method updated successfully");
            return response.IsSuccessStatusCode ? ServiceResponseHelpers.Success(true, responseMessage) : ServiceResponseHelpers.Failure<bool>(responseMessage);
        }, "Failed to Update Payment Method");
    }

    public async Task<ServiceResponse<bool>> DeletePaymentMethod(int id)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.DeleteAsync($"{_apiSettings.Endpoints!.DeletePaymentMethod}/{id}"));
            if (response is null) return ServiceResponseHelpers.Failure<bool>("Failed to connect to the API");
            
            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Payment Method deleted successfully");
            return response.IsSuccessStatusCode ? ServiceResponseHelpers.Success(true, responseMessage) : ServiceResponseHelpers.Failure<bool>(responseMessage);
        }, "Failed to Delete Payment Method");
    }
}
