using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using POS.Reports.BarcodeServices;

namespace POS.Reports.ReportsMakerServices;

public class BarcodeLabelDocument : IDocument
{
    private readonly List<BarcodeLabelItemModel> _items;
    private readonly BarcodeLabelOptions _options;

    public BarcodeLabelDocument(List<BarcodeLabelItemModel> items, BarcodeLabelOptions options)
    {
        _items = items ?? new List<BarcodeLabelItemModel>();
        _options = options ?? new BarcodeLabelOptions();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        float pageW = _options.IsVertical ? _options.HeightMm : _options.WidthMm;
        float pageH = _options.IsVertical ? _options.WidthMm : _options.HeightMm;

        // Base font scale
        float fontScale = _options.FontSizeLevel switch
        {
            "small" => 0.85f,
            "large" => 1.15f,
            _ => 1.0f
        };

        // Margin & dynamic sizing based on user options and label height
        float marginMm = Math.Max(0.4f, _options.MarginMm);
        float barcodeHeightMm = Math.Max(4.0f, Math.Min(25.0f, _options.BarcodeHeightMm));

        float storeFontSize = (_options.StoreNameFontSize > 0 ? _options.StoreNameFontSize : 7.5f) * fontScale;
        float nameFontSize = (_options.ItemNameFontSize > 0 ? _options.ItemNameFontSize : 7.5f) * fontScale;
        float barcodeTextFontSize = (_options.BarcodeTextFontSize > 0 ? _options.BarcodeTextFontSize : 6.5f) * fontScale;
        float priceFontSize = (_options.PriceFontSize > 0 ? _options.PriceFontSize : 9.0f) * fontScale;

        foreach (var item in _items)
        {
            int copies = item.Copies > 0 ? item.Copies : 1;
            
            // Barcode generation: height in pixels proportionally to physical height (1mm ~ 12px at 300 DPI)
            int pixelH = Math.Max(40, (int)(barcodeHeightMm * 11.8f));
            var barcodePng = !string.IsNullOrWhiteSpace(item.Barcode) && _options.ShowBarcode
                ? BarcodeImageHelper.GenerateBarcodePng(item.Barcode, height: pixelH, moduleWidth: _options.BarcodeModuleWidth, quietZoneModules: _options.QuietZoneModules)
                : null;

            string itemName = _options.UseEnglishName && !string.IsNullOrWhiteSpace(item.NameEn)
                ? item.NameEn
                : item.DisplayName;

            for (int c = 0; c < copies; c++)
            {
                container.Page(page =>
                {
                    page.Size(pageW, pageH, Unit.Millimetre);
                    page.Margin(marginMm, Unit.Millimetre);

                    page.Content().Column(col =>
                    {
                        // 1. Store Name
                        if (_options.ShowStoreName && !string.IsNullOrWhiteSpace(_options.StoreName))
                        {
                            col.Item().AlignCenter().Text(_options.StoreName)
                                .FontSize(storeFontSize)
                                .Bold()
                                .LineHeight(1.0f);

                            if (_options.SpaceStoreToItemMm > 0)
                                col.Item().Height(_options.SpaceStoreToItemMm, Unit.Millimetre);
                        }

                        // 2. Product Name (with top offset and controllable max lines)
                        if (_options.ShowItemName && !string.IsNullOrWhiteSpace(itemName))
                        {
                            if (_options.ItemNameTopOffsetMm > 0)
                                col.Item().Height(_options.ItemNameTopOffsetMm, Unit.Millimetre);

                            col.Item().AlignCenter().Text(itemName)
                                .FontSize(nameFontSize)
                                .Bold()
                                .LineHeight(1.0f)
                                .ClampLines(_options.ItemNameMaxLines > 0 ? _options.ItemNameMaxLines : 2);

                            if (_options.SpaceItemToBarcodeMm > 0)
                                col.Item().Height(_options.SpaceItemToBarcodeMm, Unit.Millimetre);
                        }

                        // 3. Barcode image
                        if (_options.ShowBarcode && barcodePng != null)
                        {
                            col.Item().AlignCenter()
                                .Height(barcodeHeightMm, Unit.Millimetre)
                                .Image(barcodePng)
                                .FitArea();

                            if (_options.SpaceBarcodeToTextMm > 0)
                                col.Item().Height(_options.SpaceBarcodeToTextMm, Unit.Millimetre);
                        }

                        // 4. Barcode text
                        if (_options.ShowBarcodeText && !string.IsNullOrWhiteSpace(item.Barcode))
                        {
                            col.Item().AlignCenter().Text(item.Barcode)
                                .FontSize(barcodeTextFontSize)
                                .Bold()
                                .LineHeight(1.0f);

                            if (_options.SpaceTextToPriceMm > 0)
                                col.Item().Height(_options.SpaceTextToPriceMm, Unit.Millimetre);
                        }

                        // 5. Price / Code
                        if (_options.ShowPrice || _options.ShowItemCode)
                        {
                            col.Item().AlignCenter().Row(row =>
                            {
                                if (_options.ShowPrice)
                                {
                                    row.RelativeItem().AlignCenter().Text($"{item.Price:0.##} {_options.Currency}")
                                        .FontSize(priceFontSize)
                                        .Bold()
                                        .LineHeight(1.0f);
                                }
                                if (_options.ShowItemCode)
                                {
                                    row.RelativeItem().AlignCenter().Text($"#{item.Id}")
                                        .FontSize(priceFontSize * 0.85f)
                                        .LineHeight(1.0f);
                                }
                            });
                        }
                    });
                });
            }
        }
    }
}
