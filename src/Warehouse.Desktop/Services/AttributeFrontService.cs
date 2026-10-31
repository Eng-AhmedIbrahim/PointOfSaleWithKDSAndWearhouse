using POS.Core.Entities.Item;
using POS.Core.Services.Contract.ItemServices;
using POS.Contract.Dtos.AttributeDtos;
using BlazorBase.Helpers;
using BlazorBase.API;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using POS.Core.Specifications;

namespace Warehouse.Desktop.Services;

public class AttributeFrontService : IAttributeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AttributeFrontService> _logger;

    public AttributeFrontService(IHttpClientFactory httpClientFactory, ApiSettings apiSettings, ILogger<AttributeFrontService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName ?? "systemAPI");
        _logger = logger;
    }

    public async Task<IReadOnlyList<Attributes>?> GetAllAttributeAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(ConstStringsHelper.AttributeAPIUrl);
            if (response.IsSuccessStatusCode)
            {
                var dtos = await response.Content.ReadFromJsonAsync<IReadOnlyList<AttributeToReturnDto>>();
                return dtos?.Select(dto => new Attributes
                {
                    Id = dto.Id,
                    ArabicName = dto.ArabicName ?? "",
                    EnglishName = dto.EnglishName ?? "",
                    AttributeItems = dto.AttributeItems.Select(ai => new AttributeItem
                    {
                        Id = ai.Id,
                        AppearanceIndex = ai.AppearanceIndex,
                        AttributeId = ai.AttributeId,
                        RelatedMenuItemId = ai.RelatedMenuItemId,
                        AttributeGroupId = ai.AttributeGroupId,
                        ExtraPrice = ai.ExtraPrice,
                        RelatedMenuItem = new MenuSalesItems
                        {
                            ArabicName = ai.ItemNameArabic ?? "",
                            EnglishName = ai.ItemNameEnglish ?? ""
                        }
                    }).ToList(),
                    AttributeGroups = dto.AttributeGroups.Select(ag => new AttributeGroup {
                        Id = ag.Id,
                        ArabicName = ag.ArabicName,
                        EnglishName = ag.EnglishName,
                        DisplayOrder = ag.DisplayOrder,
                        AttributeId = ag.AttributeId
                    }).ToList()
                }).ToList();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all attributes");
            return null;
        }
    }

    public async Task<Attributes?> CreateAttributeAsync(Attributes attribute)
    {
        try
        {
            var dto = new CreateAttributeDto
            {
                ArabicName = attribute.ArabicName,
                EnglishName = attribute.EnglishName,
                AttributeItems = attribute.AttributeItems.Select(ai => new AttributeItemDto {
                    AppearanceIndex = ai.AppearanceIndex,
                    RelatedMenuItemId = ai.RelatedMenuItemId,
                    AttributeGroupId = ai.AttributeGroupId,
                    ExtraPrice = ai.ExtraPrice
                }).ToList(),
                AttributeGroups = attribute.AttributeGroups.Select(ag => new AttributeGroupDto {
                    ArabicName = ag.ArabicName,
                    EnglishName = ag.EnglishName,
                    DisplayOrder = ag.DisplayOrder
                }).ToList()
            };

            var response = await _httpClient.PostAsJsonAsync(ConstStringsHelper.AttributeAPIUrl, dto);
            if (response.IsSuccessStatusCode)
            {
                var resultDto = await response.Content.ReadFromJsonAsync<AttributeToReturnDto>();
                if (resultDto != null)
                {
                    attribute.Id = resultDto.Id;
                    return attribute;
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating attribute");
            return null;
        }
    }

    public async Task<Attributes?> UpdateAttributeAsync(Attributes oldAttribute, Attributes newAttribute)
    {
        try
        {
            var dto = new UpdatedAttributeDto
            {
                Id = newAttribute.Id,
                ArabicName = newAttribute.ArabicName,
                EnglishName = newAttribute.EnglishName,
                AttributeItems = newAttribute.AttributeItems.Select(ai => new AttributeItemDto {
                    AppearanceIndex = ai.AppearanceIndex,
                    RelatedMenuItemId = ai.RelatedMenuItemId,
                    AttributeGroupId = ai.AttributeGroupId,
                    ExtraPrice = ai.ExtraPrice
                }).ToList(),
                AttributeGroups = newAttribute.AttributeGroups.Select(ag => new AttributeGroupDto {
                    Id = ag.Id,
                    ArabicName = ag.ArabicName,
                    EnglishName = ag.EnglishName,
                    DisplayOrder = ag.DisplayOrder,
                    AttributeId = ag.AttributeId
                }).ToList()
            };

            var response = await _httpClient.PutAsJsonAsync(ConstStringsHelper.AttributeAPIUrl, dto);
            if (response.IsSuccessStatusCode)
            {
                return newAttribute;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attribute");
            return null;
        }
    }

    public async Task<bool> DeleteAttribute(Attributes attribute)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{ConstStringsHelper.AttributeAPIUrl}/DeleteAttribute/{attribute.Id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attribute");
            return false;
        }
    }

    public async Task<Attributes?> GetAttributeByIdAsync(int attributeId)
    {
         _logger.LogWarning("GetAttributeByIdAsync called but not fully implemented (Specs preferred)");
         return null;
    }

    public Task<Attributes?> GetAttributeByIdWithSpecAsync(ISpecifications<Attributes> attributeSpecifications) => Task.FromResult<Attributes?>(null);
    public Task<IReadOnlyList<Attributes>?> GetAllAttributeWithSpecsAsync(ISpecifications<Attributes> attributeSpecifications) => Task.FromResult<IReadOnlyList<Attributes>?>(null);
    public Task<ICollection<AttributeItem>?> AddAttributeItems(ICollection<AttributeItem> attributeItems) => Task.FromResult<ICollection<AttributeItem>?>(null);
    public Task<bool> DeleteAttributeItem(AttributeItem attributeItem) => Task.FromResult(false);
    public Task<AttributeItem?> GetAttributeItemByIdAsync(int attributeItemId) => Task.FromResult<AttributeItem?>(null);
}

