using System;
using System.Linq;
using System.Threading.Tasks;
using BlazorBase;
using BlazorBase.ERPFrontServices.BranchServices;

namespace Warehouse.Desktop.Services;

/// <summary>
/// Service to dynamically resolve branch currency from database/API settings (e.g. SAR -> ر.س, EGP -> ج.م)
/// </summary>
public class WarehouseCurrencyService
{
    private readonly IBranchService _branchService;
    private readonly CommonProperties _commonProperties;
    private string? _cachedCurrencySymbol;
    private string? _cachedCurrencyCode;

    public WarehouseCurrencyService(IBranchService branchService, CommonProperties commonProperties)
    {
        _branchService = branchService;
        _commonProperties = commonProperties;
    }

    /// <summary>
    /// Gets the localized currency symbol (e.g. "ج.م" or "ر.س")
    /// </summary>
    public async Task<string> GetCurrencySymbolAsync()
    {
        if (!string.IsNullOrEmpty(_cachedCurrencySymbol))
            return _cachedCurrencySymbol;

        await ResolveCurrencyAsync();
        return _cachedCurrencySymbol ?? "ج.م";
    }

    /// <summary>
    /// Gets the raw currency code (e.g. "EGP", "SAR", "USD")
    /// </summary>
    public async Task<string> GetCurrencyCodeAsync()
    {
        if (!string.IsNullOrEmpty(_cachedCurrencyCode))
            return _cachedCurrencyCode;

        await ResolveCurrencyAsync();
        return _cachedCurrencyCode ?? "EGP";
    }

    private async Task ResolveCurrencyAsync()
    {
        try
        {
            // 1. Check if branch details already loaded in CommonProperties
            if (_commonProperties?.BranchDetails != null && !string.IsNullOrWhiteSpace(_commonProperties.BranchDetails.Currency))
            {
                _cachedCurrencyCode = _commonProperties.BranchDetails.Currency.Trim();
                _cachedCurrencySymbol = MapToSymbol(_cachedCurrencyCode);
                return;
            }

            // 2. Query branch service
            var branches = await _branchService.GetBranches();
            var activeBranch = branches.FirstOrDefault(b => b.Active) ?? branches.FirstOrDefault();
            if (activeBranch != null)
            {
                if (_commonProperties != null)
                {
                    _commonProperties.BranchDetails = activeBranch;
                }

                if (!string.IsNullOrWhiteSpace(activeBranch.Currency))
                {
                    _cachedCurrencyCode = activeBranch.Currency.Trim();
                    _cachedCurrencySymbol = MapToSymbol(_cachedCurrencyCode);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WarehouseCurrencyService] Failed to fetch currency: {ex.Message}");
        }

        _cachedCurrencyCode = "EGP";
        _cachedCurrencySymbol = "ج.م";
    }

    public static string MapToSymbol(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "ج.م";
        code = code.Trim();

        return code.ToUpperInvariant() switch
        {
            "EGP" => "ج.م",
            "SAR" => "ر.س",
            "AED" => "د.إ",
            "KWD" => "د.ك",
            "QAR" => "ر.ق",
            "BHD" => "د.ب",
            "OMR" => "ر.ع",
            "JOD" => "د.أ",
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            _ => code // If already in Arabic like "ر.س" or "ج.م"
        };
    }
}
