using System.Collections.Generic;

namespace POS.Reports.BarcodeServices;

public class BarcodeLabelItemModel
{
    public int Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Copies { get; set; } = 1;

    public string DisplayName => !string.IsNullOrWhiteSpace(NameAr) ? NameAr : NameEn;
}

public class BarcodeLabelOptions
{
    public string StoreName { get; set; } = "";
    public bool ShowStoreName { get; set; } = true;
    public bool ShowItemName { get; set; } = true;
    public bool ShowPrice { get; set; } = true;
    public bool ShowBarcode { get; set; } = true;
    public bool ShowBarcodeText { get; set; } = true;
    public bool ShowItemCode { get; set; } = false;
    public string Currency { get; set; } = "ج.م";
    
    // Label dimensions in Millimetres
    public float WidthMm { get; set; } = 40f;
    public float HeightMm { get; set; } = 25f;
    
    // Orientation: if true, Portrait/Vertical, else Landscape
    public bool IsVertical { get; set; } = false;
    
    // Font sizing preset: "small", "medium", "large"
    public string FontSizeLevel { get; set; } = "medium";
    public bool UseEnglishName { get; set; } = false;

    // --- Advanced Grid & Spacing Controls (mm) ---
    public float MarginMm { get; set; } = 0.6f;
    public float SpaceStoreToItemMm { get; set; } = 0.3f;
    public float SpaceItemToBarcodeMm { get; set; } = 0.4f;
    public float SpaceBarcodeToTextMm { get; set; } = 0.3f;
    public float SpaceTextToPriceMm { get; set; } = 0.4f;

    // Item name offset & wrapping
    public float ItemNameTopOffsetMm { get; set; } = 0f;
    public int ItemNameMaxLines { get; set; } = 2;

    // Barcode dimensions & UPOS Scanner Optimization
    public float BarcodeHeightMm { get; set; } = 7.5f;
    public int BarcodeModuleWidth { get; set; } = 2; // 2 for crisp 203/300 DPI thermal
    public int QuietZoneModules { get; set; } = 10; // ISO standard 10X for 100% UPOS readability

    // Direct font sizes (pt)
    public float StoreNameFontSize { get; set; } = 6.5f;
    public float ItemNameFontSize { get; set; } = 7.0f;
    public float BarcodeTextFontSize { get; set; } = 6.0f;
    public float PriceFontSize { get; set; } = 8.5f;
}
