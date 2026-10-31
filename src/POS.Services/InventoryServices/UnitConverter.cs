using POS.Core.Entities.Item;

namespace POS.Services.InventoryServices;

public static class UnitConverter
{
    public static decimal Convert(decimal quantity, Unit? sourceUnit, Unit? targetUnit)
    {
        if (sourceUnit == null || targetUnit == null)
            return quantity;

        if (sourceUnit.Id == targetUnit.Id)
            return quantity;

        var sourceCode = sourceUnit.Code?.Trim().ToLowerInvariant() ?? "";
        var targetCode = targetUnit.Code?.Trim().ToLowerInvariant() ?? "";
        var sourceAr = sourceUnit.ArabicName?.Trim() ?? "";
        var targetAr = targetUnit.ArabicName?.Trim() ?? "";

        // Gram to Kilo
        if (IsGram(sourceCode, sourceAr) && IsKilo(targetCode, targetAr))
            return quantity / 1000m;

        // Kilo to Gram
        if (IsKilo(sourceCode, sourceAr) && IsGram(targetCode, targetAr))
            return quantity * 1000m;

        // Milliliter to Liter
        if (IsMl(sourceCode, sourceAr) && IsLiter(targetCode, targetAr))
            return quantity / 1000m;

        // Liter to Milliliter
        if (IsLiter(sourceCode, sourceAr) && IsMl(targetCode, targetAr))
            return quantity * 1000m;

        return quantity;
    }

    private static bool IsGram(string code, string ar) => 
        code == "g" || code == "gram" || code == "gm" || code == "جم" || ar == "جرام" || ar == "جم";

    private static bool IsKilo(string code, string ar) => 
        code == "kg" || code == "kilo" || code == "kilogram" || code == "كجم" || ar == "كيلو" || ar == "كجم";

    private static bool IsMl(string code, string ar) => 
        code == "ml" || code == "milliliter" || ar == "مل" || ar == "ملي" || ar == "مليلتر";

    private static bool IsLiter(string code, string ar) => 
        code == "l" || code == "liter" || code == "ltr" || ar == "لتر";
}
