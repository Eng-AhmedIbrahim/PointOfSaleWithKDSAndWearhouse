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

        foreach (var item in _items)
        {
            int copies = item.Copies > 0 ? item.Copies : 1;
            var barcodePng = !string.IsNullOrWhiteSpace(item.Barcode) && _options.ShowBarcode
                ? BarcodeImageHelper.GenerateBarcodePng(item.Barcode, height: 60, moduleWidth: 2)
                : null;

            string itemName = _options.UseEnglishName && !string.IsNullOrWhiteSpace(item.NameEn)
                ? item.NameEn
                : item.DisplayName;

            for (int c = 0; c < copies; c++)
            {
                container.Page(page =>
                {
                    page.Size(pageW, pageH, Unit.Millimetre);
                    page.Margin(1.0f, Unit.Millimetre);

                    page.Content().Column(col =>
                    {
                        col.Spacing(1);

                        // 1. Store Name
                        if (_options.ShowStoreName && !string.IsNullOrWhiteSpace(_options.StoreName))
                        {
                            col.Item().AlignCenter().Text(_options.StoreName)
                                .FontSize(8f * fontScale)
                                .Bold();
                        }

                        // 2. Product Name
                        if (_options.ShowItemName && !string.IsNullOrWhiteSpace(itemName))
                        {
                            col.Item().AlignCenter().Text(itemName)
                                .FontSize(7.5f * fontScale)
                                .Bold()
                                .ClampLines(2);
                        }

                        // 3. Barcode image
                        if (_options.ShowBarcode && barcodePng != null)
                        {
                            col.Item().AlignCenter().MaxHeight(10, Unit.Millimetre).Image(barcodePng);
                        }

                        // 4. Barcode text
                        if (_options.ShowBarcodeText && !string.IsNullOrWhiteSpace(item.Barcode))
                        {
                            col.Item().AlignCenter().Text(item.Barcode)
                                .FontSize(7.5f * fontScale)
                                .Bold();
                        }

                        // 5. Price / Code
                        if (_options.ShowPrice || _options.ShowItemCode)
                        {
                            col.Item().AlignCenter().Row(row =>
                            {
                                if (_options.ShowPrice)
                                {
                                    row.RelativeItem().AlignCenter().Text($"{item.Price:0.##} {_options.Currency}")
                                        .FontSize(9f * fontScale)
                                        .Bold();
                                }
                                if (_options.ShowItemCode)
                                {
                                    row.RelativeItem().AlignCenter().Text($"#{item.Id}")
                                        .FontSize(7f * fontScale);
                                }
                            });
                        }
                    });
                });
            }
        }
    }
}
