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
    public float WidthMm { get; set; } = 38f;
    public float HeightMm { get; set; } = 25f;
    
    // Orientation: if true, Portrait/Vertical, else Landscape
    public bool IsVertical { get; set; } = false;
    
    // Font sizing: "small", "medium", "large"
    public string FontSizeLevel { get; set; } = "medium";
    public bool UseEnglishName { get; set; } = false;
}
