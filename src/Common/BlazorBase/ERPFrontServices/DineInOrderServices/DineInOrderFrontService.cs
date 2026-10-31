using System.Net.Http.Json;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using POS.Contract.Dtos.DineIn;
using BlazorBase.API;
using BlazorBase.Helpers;

namespace BlazorBase.ERPFrontServices.DineInOrderServices;

public class DineInOrderFrontService : IDineInOrderFrontService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<DineInOrderFrontService> _logger;

    public DineInOrderFrontService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<DineInOrderFrontService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
        _apiSettings = apiSettings;
        _logger = logger;
    }

    public async Task<DineInOrderDto?> CreateDineInOrderAsync(DineInOrderDto order)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.CreateDineInOrder!, order);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create DineIn order");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<DineInOrderDto>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating DineIn order");
            return null;
        }
    }

    public async Task<DineInOrderDto?> UpdateDineInOrderAsync(DineInOrderDto order)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(_apiSettings.Endpoints!.UpdateDineInOrder!, order);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to update DineIn order");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<DineInOrderDto>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DineIn order");
            return null;
        }
    }

    public async Task<DineInOrderDto?> GetDineInOrderByIdAsync(int orderId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/DineInOrder/{orderId}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<DineInOrderDto>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DineIn order by ID");
            return null;
        }
    }

    public async Task<DineInOrderDto?> GetDineInOrderByTableIdAsync(int tableId, string state = "Open")
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetDineInOrderByTableId}/{tableId}?state={state}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<DineInOrderDto>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DineIn order by table ID");
            return null;
        }
    }

    public async Task<IReadOnlyList<DineInOrderDto>> GetOpenOrdersByTableIdAsync(int tableId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetOpenOrdersByTableId}/{tableId}");
            if (!response.IsSuccessStatusCode)
            {
                return new List<DineInOrderDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<List<DineInOrderDto>>(content) ?? new List<DineInOrderDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting open DineIn orders by table ID");
            return new List<DineInOrderDto>();
        }
    }

    public async Task<IReadOnlyList<DineInOrderDto>> GetAllOpenDineInOrdersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllOpenDineInOrders!);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get all open DineIn orders");
                return new List<DineInOrderDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<List<DineInOrderDto>>(content) ?? new List<DineInOrderDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all open DineIn orders");
            return new List<DineInOrderDto>();
        }
    }

    public async Task<bool> CloseDineInOrderAsync(int orderId, decimal? paid = null, decimal? remain = null)
    {
        try
        {
            var url = $"{_apiSettings.Endpoints!.CloseDineInOrder}/{orderId}";
            
            // Add query parameters if provided
            var queryParams = new List<string>();
            if (paid.HasValue)
                queryParams.Add($"paid={paid.Value}");
            if (remain.HasValue)
                queryParams.Add($"remain={remain.Value}");
                
            if (queryParams.Any())
                url += "?" + string.Join("&", queryParams);
                
            var response = await _httpClient.PutAsync(url, null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing DineIn order");
            return false;
        }
    }

    public async Task<bool> VoidDineInOrderAsync(int orderId, string reason, string voidBy, string voidByName)
    {
        try
        {
            var request = new
            {
                OrderId = orderId,
                Reason = reason,
                VoidBy = voidBy,
                VoidByName = voidByName
            };
            var response = await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.VoidDineInOrder!, request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voiding DineIn order");
            return false;
        }
    }

    public async Task<bool> AddItemsToDineInOrderAsync(int dineInOrderId, List<OrderItemsDetailsDto> items)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiSettings.Endpoints!.AddItemsToDineInOrder}/{dineInOrderId}/items", items);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding items to DineIn order");
            return false;
        }
    }

    public async Task<bool> UpdateDineInOrderDiscountAsync(int dineInOrderId, decimal? discountAmount, decimal? discountPercentage, string? discountType, string? discountReason)
    {
        try
        {
            var request = new
            {
                DiscountAmount = discountAmount,
                DiscountPercentage = discountPercentage,
                DiscountType = discountType,
                DiscountReason = discountReason
            };

            var response = await _httpClient.PutAsJsonAsync($"{_apiSettings.Endpoints!.UpdateDineInOrderDiscount}/{dineInOrderId}/discount", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DineIn order discount");
            return false;
        }
    }

    public async Task<bool> TransferDineInOrderAsync(int orderId, int newTableId, string newTableName)
    {
        try
        {
            var response = await _httpClient.PutAsync($"{_apiSettings.Endpoints!.TransferDineInOrder}/{orderId}/{newTableId}/{newTableName}", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring DineIn order");
            return false;
        }
    }

    public async Task<bool> MergeDineInOrdersAsync(int primaryOrderId, List<int> secondaryOrderIds)
    {
        try
        {
            var request = new
            {
                PrimaryOrderId = primaryOrderId,
                SecondaryOrderIds = secondaryOrderIds
            };

            var response = await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.MergeDineInOrders!, request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging DineIn orders");
            return false;
        }
    }

    public async Task<bool> SplitDineInOrderAsync(int sourceOrderId, List<SplitTargetDto> targets)
    {
        try
        {
            var request = new
            {
                SourceOrderId = sourceOrderId,
                Targets = targets
            };

            var response = await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.SplitDineInOrder!, request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error splitting DineIn order");
            return false;
        }
    }

    public async Task<bool> VoidDineInItemsAsync(int orderId, List<OrderItemVoidDto> itemsToVoid, string reason, string voidBy, string voidByName)
    {
        try
        {
            var request = new
            {
                OrderId = orderId,
                ItemsToVoid = itemsToVoid,
                Reason = reason,
                VoidBy = voidBy,
                VoidByName = voidByName
            };

            var response = await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.VoidDineInItems!, request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voiding DineIn order items");
            return false;
        }
    }

    public async Task<int> IncrementPrintCountAsync(int orderId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/DineInOrder/{orderId}/increment-print", null);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return int.Parse(content);
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing print count");
            return 0;
        }
    }

    public async Task<bool> ReserveTableAsync(int tableId, DineInOrderDto reservationDetails)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/DineInOrder/reserve", reservationDetails);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving table {TableId}", tableId);
            return false;
        }
    }

    public async Task<bool> CancelReservationAsync(int tableId)
    {
        try
        {
            // Find the reservation order for this table
            var reservation = await GetDineInOrderByTableIdAsync(tableId, "Reserved");
            if (reservation == null)
            {
                _logger.LogWarning("No reservation found for table {TableId}", tableId);
                return false;
            }

            var response = await _httpClient.DeleteAsync($"api/DineInOrder/cancel-reservation/{reservation.Id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling reservation for table {TableId}", tableId);
            return false;
        }
    }

    public async Task<bool> SeatReservationAsync(int orderId, string captainId, string captainName)
    {
        try
        {
            var request = new { CaptainId = captainId, CaptainName = captainName };
            var response = await _httpClient.PostAsJsonAsync($"api/DineInOrder/seat-reservation/{orderId}", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seating reservation {OrderId}", orderId);
            return false;
        }
    }
}
