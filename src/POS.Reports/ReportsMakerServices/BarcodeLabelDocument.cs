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

        // Font scale
        float fontScale = _options.FontSizeLevel switch
        {
            "small" => 0.85f,
            "large" => 1.15f,
            _ => 1.0f
        };

        float sideMarginMm = Math.Max(0.4f, _options.MarginMm);
        float topMarginMm  = 2.0f; // 2mm from top as user requested

        float barcodeHeightMm   = Math.Max(4.0f, Math.Min(20.0f, _options.BarcodeHeightMm));
        float storeFontSize     = (_options.StoreNameFontSize  > 0 ? _options.StoreNameFontSize  : 9.0f)  * fontScale;
        float nameFontSize      = (_options.ItemNameFontSize   > 0 ? _options.ItemNameFontSize   : 7.0f)  * fontScale;
        float barcodeTextFont   = (_options.BarcodeTextFontSize> 0 ? _options.BarcodeTextFontSize: 6.0f)  * fontScale;
        float priceFontSize     = (_options.PriceFontSize      > 0 ? _options.PriceFontSize      : 8.5f)  * fontScale;

        foreach (var item in _items)
        {
            int copies = item.Copies > 0 ? item.Copies : 1;

            // Generate barcode PNG at proper resolution
            int pixelH = Math.Max(60, (int)(barcodeHeightMm * 11.8f));
            var barcodePng = !string.IsNullOrWhiteSpace(item.Barcode) && _options.ShowBarcode
                ? BarcodeImageHelper.GenerateBarcodePng(item.Barcode,
                    height: pixelH,
                    moduleWidth: _options.BarcodeModuleWidth,
                    quietZoneModules: _options.QuietZoneModules)
                : null;

            // Raw item name
            string itemName = _options.UseEnglishName && !string.IsNullOrWhiteSpace(item.NameEn)
                ? item.NameEn
                : item.DisplayName;

            // Fix Arabic BiDi — prepend RTL mark so size codes (XXL, XL…) stay at visual END
            string displayName = !string.IsNullOrWhiteSpace(itemName)
                ? "\u200F" + itemName
                : itemName;

            for (int c = 0; c < copies; c++)
            {
                container.Page(page =>
                {
                    page.Size(pageW, pageH, Unit.Millimetre);

                    // 2 mm from top, tight sides & bottom
                    page.MarginTop(topMarginMm, Unit.Millimetre);
                    page.MarginRight(sideMarginMm, Unit.Millimetre);
                    page.MarginBottom(sideMarginMm, Unit.Millimetre);
                    page.MarginLeft(sideMarginMm, Unit.Millimetre);

                    page.Content().Column(col =>
                    {
                        // ── 1. Store Name ────────────────────────────────────
                        if (_options.ShowStoreName && !string.IsNullOrWhiteSpace(_options.StoreName))
                        {
                            col.Item().AlignCenter().Text(_options.StoreName)
                                .FontSize(storeFontSize)
                                .Bold()
                                .LineHeight(1.0f);

                            if (_options.SpaceStoreToItemMm > 0)
                                col.Item().Height(_options.SpaceStoreToItemMm, Unit.Millimetre);
                        }

                        // ── 2. Product Name (RTL-safe, max 2 lines) ──────────
                        if (_options.ShowItemName && !string.IsNullOrWhiteSpace(itemName))
                        {
                            if (_options.ItemNameTopOffsetMm > 0)
                                col.Item().Height(_options.ItemNameTopOffsetMm, Unit.Millimetre);

                            col.Item().AlignCenter().Text(displayName)
                                .FontSize(nameFontSize)
                                .Bold()
                                .LineHeight(1.0f)
                                .ClampLines(_options.ItemNameMaxLines > 0 ? _options.ItemNameMaxLines : 2);

                            if (_options.SpaceItemToBarcodeMm > 0)
                                col.Item().Height(_options.SpaceItemToBarcodeMm, Unit.Millimetre);
                        }

                        // ── 3. Barcode Image ─────────────────────────────────
                        if (_options.ShowBarcode && barcodePng != null)
                        {
                            col.Item().AlignCenter()
                                .Height(barcodeHeightMm, Unit.Millimetre)
                                .Image(barcodePng)
                                .FitArea();

                            if (_options.SpaceBarcodeToTextMm > 0)
                                col.Item().Height(_options.SpaceBarcodeToTextMm, Unit.Millimetre);
                        }

                        // ── 4. Barcode Number Text ───────────────────────────
                        if (_options.ShowBarcodeText && !string.IsNullOrWhiteSpace(item.Barcode))
                        {
                            col.Item().AlignCenter().Text(item.Barcode)
                                .FontSize(barcodeTextFont)
                                .Bold()
                                .LineHeight(1.0f);

                            if (_options.SpaceTextToPriceMm > 0)
                                col.Item().Height(_options.SpaceTextToPriceMm, Unit.Millimetre);
                        }

                        // ── 5. Price / Code ───────────────────────────────────
                        if (_options.ShowPrice || _options.ShowItemCode)
                        {
                            col.Item().AlignCenter().Row(row =>
                            {
                                if (_options.ShowPrice)
                                {
                                    row.RelativeItem().AlignCenter()
                                        .Text($"{item.Price:0.##} {_options.Currency}")
                                        .FontSize(priceFontSize)
                                        .Bold()
                                        .LineHeight(1.0f);
                                }
                                if (_options.ShowItemCode)
                                {
                                    row.RelativeItem().AlignCenter()
                                        .Text($"#{item.Id}")
                                        .FontSize(priceFontSize * 0.85f)
                                        .LineHeight(1.0f);
                                }
                            });
                        }

                        // ── Fill remaining vertical space (prevents blank gap at top) ──
                        col.Item().Extend();
                    });
                });
            }
        }
    }
}
