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

        // Margin & dynamic sizing based on label height
        float marginMm = pageH <= 26f ? 0.6f : 1.0f;
        float barcodeHeightMm;
        float storeFontSize;
        float nameFontSize;
        float barcodeTextFontSize;
        float priceFontSize;

        if (pageH <= 26f) // Small labels (e.g. 38x25mm)
        {
            barcodeHeightMm = 6.0f;
            storeFontSize = 6.5f * fontScale;
            nameFontSize = 6.5f * fontScale;
            barcodeTextFontSize = 6.0f * fontScale;
            priceFontSize = 7.5f * fontScale;
        }
        else if (pageH <= 32f) // Medium labels (e.g. 40x30, 50x30)
        {
            barcodeHeightMm = 8.5f;
            storeFontSize = 7.5f * fontScale;
            nameFontSize = 7.5f * fontScale;
            barcodeTextFontSize = 7.0f * fontScale;
            priceFontSize = 8.5f * fontScale;
        }
        else // Large labels (e.g. 60x40)
        {
            barcodeHeightMm = 12.0f;
            storeFontSize = 8.5f * fontScale;
            nameFontSize = 8.5f * fontScale;
            barcodeTextFontSize = 8.0f * fontScale;
            priceFontSize = 10.0f * fontScale;
        }

        foreach (var item in _items)
        {
            int copies = item.Copies > 0 ? item.Copies : 1;
            var barcodePng = !string.IsNullOrWhiteSpace(item.Barcode) && _options.ShowBarcode
                ? BarcodeImageHelper.GenerateBarcodePng(item.Barcode, height: 50, moduleWidth: 2, quietZoneModules: 6)
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
                        col.Spacing(0.5f);

                        // 1. Store Name
                        if (_options.ShowStoreName && !string.IsNullOrWhiteSpace(_options.StoreName))
                        {
                            col.Item().AlignCenter().Text(_options.StoreName)
                                .FontSize(storeFontSize)
                                .Bold()
                                .LineHeight(1.0f);
                        }

                        // 2. Product Name
                        if (_options.ShowItemName && !string.IsNullOrWhiteSpace(itemName))
                        {
                            col.Item().AlignCenter().Text(itemName)
                                .FontSize(nameFontSize)
                                .Bold()
                                .LineHeight(1.0f)
                                .ClampLines(pageH <= 26f && _options.ShowStoreName ? 1 : 2);
                        }

                        // 3. Barcode image
                        if (_options.ShowBarcode && barcodePng != null)
                        {
                            col.Item().AlignCenter()
                                .Height(barcodeHeightMm, Unit.Millimetre)
                                .Image(barcodePng)
                                .FitArea();
                        }

                        // 4. Barcode text
                        if (_options.ShowBarcodeText && !string.IsNullOrWhiteSpace(item.Barcode))
                        {
                            col.Item().AlignCenter().Text(item.Barcode)
                                .FontSize(barcodeTextFontSize)
                                .Bold()
                                .LineHeight(1.0f);
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
