using POS.Contract.Dtos.InventoryDtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorBase.Helpers;

public enum UnitFamily
{
    Unknown,
    Weight,
    Volume,
    Count,
    Length
}

public class WarehouseProductLookupDto
{
    public int Id { get; set; }
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public int BaseUnitId { get; set; }
    public string? BaseUnitName { get; set; }
    public List<WarehouseProductUnitLookupDto> Units { get; set; } = new();
}

public class WarehouseProductUnitLookupDto
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public string? UnitName { get; set; }
    public decimal ConversionFactorToBase { get; set; }
}

public static class InventoryUnitHelper
{
    public static UnitFamily GetUnitFamily(UnitDto? unit)
    {
        if (unit == null) return UnitFamily.Unknown;

        string code = (unit.Code ?? "").Trim().ToLowerInvariant();
        string ar = (unit.ArabicName ?? "").Trim();

        // Weight
        if (code is "kg" or "g" or "gm" or "mg" or "ton" or "bag" ||
            ar.Contains("كيلو") || ar.Contains("كجم") || ar.Contains("جرام") || 
            ar.Contains("جم") || ar.Contains("طن") || ar.Contains("شيكارة") || ar.Contains("كيس"))
        {
            return UnitFamily.Weight;
        }

        // Volume
        if (code is "l" or "ltr" or "ml" or "gal" or "bbl" ||
            ar.Contains("لتر") || ar.Contains("مل") || ar.Contains("جالون") || ar.Contains("برميل"))
        {
            return UnitFamily.Volume;
        }

        // Length
        if (code is "m" or "cm" or "mm" ||
            ar.Contains("متر") || ar.Contains("سنتيمتر") || ar.Contains("مليمتر"))
        {
            return UnitFamily.Length;
        }

        // Count / Piece
        if (code is "pcs" or "u" or "box" or "ctn" or "doz" or "pk" or "jar" or "tin" or "roll" or "pch" or "bdl" or "bar" or "crt" or "plt" ||
            ar.Contains("قطعة") || ar.Contains("حبة") || ar.Contains("علبة") || ar.Contains("كرتونة") || 
            ar.Contains("دستة") || ar.Contains("باكت") || ar.Contains("برطمان") || ar.Contains("صفيحة") || 
            ar.Contains("كان") || ar.Contains("رول") || ar.Contains("حزمة") || ar.Contains("قالب") || 
            ar.Contains("صندوق") || ar.Contains("قفص") || ar.Contains("بالتة"))
        {
            return UnitFamily.Count;
        }

        return UnitFamily.Unknown;
    }

    public static decimal GetFactorToBase(UnitDto? unit, WarehouseProductLookupDto? whProduct = null)
    {
        if (unit == null) return 1.0m;

        string code = (unit.Code ?? "").Trim().ToLowerInvariant();
        string ar = (unit.ArabicName ?? "").Trim();

        // Check if there is a warehouse product override
        if (whProduct != null)
        {
            // If it matches base unit name
            if (!string.IsNullOrWhiteSpace(whProduct.BaseUnitName) && 
                (whProduct.BaseUnitName.Trim() == ar || (whProduct.BaseUnitName.Contains("كيلو") && ar.Contains("كيلو"))))
            {
                return 1.0m;
            }

            // If it matches a configured secondary unit in WarehouseProductUnits
            var matchingWhUnit = whProduct.Units?.FirstOrDefault(u => 
                !string.IsNullOrWhiteSpace(u.UnitName) && (u.UnitName.Trim() == ar || ar.Contains(u.UnitName.Trim()) || u.UnitName.Trim().Contains(ar)));
            if (matchingWhUnit != null && matchingWhUnit.ConversionFactorToBase > 0)
            {
                return matchingWhUnit.ConversionFactorToBase;
            }

            // Check standard metric conversions relative to base unit
            bool baseIsKilo = (whProduct.BaseUnitName ?? "").Contains("كيلو");
            if (baseIsKilo)
            {
                if (code == "g" || ar.Contains("جرام")) return 0.001m;
                if (code == "mg" || ar.Contains("ملي جرام")) return 0.000001m;
                if (code == "ton" || ar.Contains("طن")) return 1000m;
            }

            bool baseIsLiter = (whProduct.BaseUnitName ?? "").Contains("لتر");
            if (baseIsLiter)
            {
                if (code == "ml" || ar.Contains("مل")) return 0.001m;
                if (code == "gal" || ar.Contains("جالون")) return 3.785m;
            }
        }

        // Standard Default Conversion Factors
        // Weight (base: kg)
        if (code == "ton" || ar.Contains("طن")) return 1000m;
        if (code == "kg" || ar.Contains("كيلو") || ar.Contains("كجم")) return 1.0m;
        if (code == "g" || code == "gm" || ar.Contains("جرام") || ar == "جم") return 0.001m;
        if (code == "mg" || ar.Contains("ملي جرام")) return 0.000001m;
        if (code == "bag" || ar.Contains("شيكارة") || ar.Contains("كيس")) return 25m; // standard bag

        // Volume (base: L)
        if (code == "bbl" || ar.Contains("برميل")) return 159m;
        if (code == "gal" || ar.Contains("جالون")) return 3.785m;
        if (code == "l" || code == "ltr" || ar.Contains("لتر")) return 1.0m;
        if (code == "ml" || ar.Contains("مل")) return 0.001m;

        // Count (base: pcs)
        if (code == "plt" || ar.Contains("بالتة")) return 100m;
        if (code == "crt" || ar.Contains("صندوق") || ar.Contains("قفص")) return 24m;
        if (code == "ctn" || ar.Contains("كرتونة")) return 24m;
        if (code == "doz" || ar.Contains("دستة")) return 12m;
        if (code == "pk" || ar.Contains("باكت")) return 10m;
        if (code == "box" || ar.Contains("علبة")) return 1m;
        if (code == "pcs" || code == "u" || ar.Contains("قطعة") || ar.Contains("حبة")) return 1.0m;

        // Length (base: m)
        if (code == "m" || ar.Contains("متر")) return 1.0m;
        if (code == "cm" || ar.Contains("سنتيمتر")) return 0.01m;
        if (code == "mm" || ar.Contains("مليمتر")) return 0.001m;

        return 1.0m;
    }

    public static decimal ConvertQuantity(decimal quantity, UnitDto? fromUnit, UnitDto? toUnit, WarehouseProductLookupDto? whProduct = null)
    {
        if (fromUnit == null || toUnit == null || fromUnit.Id == toUnit.Id || quantity == 0)
            return quantity;

        var fromFamily = GetUnitFamily(fromUnit);
        var toFamily = GetUnitFamily(toUnit);

        // If both units belong to known families but are different families (e.g. weight vs length)
        if (fromFamily != UnitFamily.Unknown && toFamily != UnitFamily.Unknown && fromFamily != toFamily)
        {
            // Incompatible families, cannot convert mathematically
            return quantity;
        }

        decimal fromFactor = GetFactorToBase(fromUnit, whProduct);
        decimal toFactor = GetFactorToBase(toUnit, whProduct);

        if (fromFactor > 0 && toFactor > 0)
        {
            decimal inBase = quantity * fromFactor;
            return Math.Round(inBase / toFactor, 4);
        }

        return quantity;
    }

    public static List<UnitDto> GetAllowedUnits(UnitDto? currentUnit, List<UnitDto> allUnits, WarehouseProductLookupDto? whProduct = null)
    {
        if (allUnits == null || allUnits.Count == 0) return new List<UnitDto>();

        // 1. If warehouse product is linked
        if (whProduct != null)
        {
            var allowed = new List<UnitDto>();

            // Include base unit
            var baseU = allUnits.FirstOrDefault(u => 
                !string.IsNullOrWhiteSpace(whProduct.BaseUnitName) && 
                ((u.ArabicName ?? "").Trim() == whProduct.BaseUnitName.Trim() || 
                 (whProduct.BaseUnitName.Contains("كيلو") && (u.ArabicName ?? "").Contains("كيلو"))));
            if (baseU != null) allowed.Add(baseU);

            // Include secondary units configured in WarehouseProductUnits
            if (whProduct.Units != null)
            {
                foreach (var pu in whProduct.Units)
                {
                    if (string.IsNullOrWhiteSpace(pu.UnitName)) continue;
                    var matchU = allUnits.FirstOrDefault(u => 
                        (u.ArabicName ?? "").Trim() == pu.UnitName.Trim() ||
                        (u.ArabicName ?? "").Contains(pu.UnitName.Trim()) ||
                        pu.UnitName.Trim().Contains((u.ArabicName ?? "").Trim()));
                    if (matchU != null && !allowed.Any(x => x.Id == matchU.Id))
                    {
                        allowed.Add(matchU);
                    }
                }
            }

            // Include standard sub/super metric units for the base unit family
            var baseFamily = GetUnitFamily(baseU ?? currentUnit);
            if (baseFamily != UnitFamily.Unknown)
            {
                var standardUnits = allUnits.Where(u => {
                    var fam = GetUnitFamily(u);
                    if (fam != baseFamily) return false;
                    string code = (u.Code ?? "").ToLowerInvariant();
                    string ar = u.ArabicName ?? "";
                    if (baseFamily == UnitFamily.Weight)
                        return code is "kg" or "g" or "ton" || ar.Contains("كيلو") || ar.Contains("جرام") || ar.Contains("طن");
                    if (baseFamily == UnitFamily.Volume)
                        return code is "l" or "ml" || ar.Contains("لتر") || ar.Contains("مل");
                    return true;
                });

                foreach (var su in standardUnits)
                {
                    if (!allowed.Any(x => x.Id == su.Id)) allowed.Add(su);
                }
            }

            if (allowed.Count > 0)
            {
                // Ensure currentUnit is included if set
                if (currentUnit != null && !allowed.Any(x => x.Id == currentUnit.Id))
                    allowed.Add(currentUnit);

                return allowed;
            }
        }

        // 2. Fallback: Filter by unit family
        if (currentUnit != null)
        {
            var family = GetUnitFamily(currentUnit);
            if (family != UnitFamily.Unknown)
            {
                var filtered = allUnits.Where(u => GetUnitFamily(u) == family).ToList();
                if (filtered.Count > 0) return filtered;
            }
        }

        return allUnits;
    }
}
