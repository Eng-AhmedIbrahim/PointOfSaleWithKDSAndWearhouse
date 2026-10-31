using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System;
using POS.Contract.Dtos.ReportingDtos;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficeInventoryReportDocument : IDocument
{
    private readonly List<InventorySummaryDto> _inventory;
    private readonly string _branchName;
    private readonly DateTime _reportDate;
    private readonly string _reportTitle;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficeInventoryReportDocument(List<InventorySummaryDto> inventory, string branchName, DateTime reportDate, string title = "تقرير حالة المخزن", string lang = "ar", bool isThermal = false)
    {
        _inventory = inventory ?? new List<InventorySummaryDto>();
        _branchName = branchName;
        _reportDate = reportDate;
        _reportTitle = title ?? "تقرير حالة المخزن";
        _isEnglish = (lang ?? "ar").ToLower() == "en";
        _isThermal = isThermal;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            if (_isThermal)
            {
                page.ContinuousSize(8f, Unit.Centimetre);
                page.Margin(2, Unit.Millimetre);
            }
            else
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
            }

            page.PageColor(Colors.White);
            
            var baseStyle = TextStyle.Default.FontFamily("Arial", "Noto Sans Arabic").FontSize(_isThermal ? 8 : 10);
            page.DefaultTextStyle(baseStyle);

            // Important: Enable RTL for proper column alignment (ID on right, Min Qty on left)
            if (!_isEnglish)
                page.ContentFromRightToLeft();

            page.Content().Column(col =>
            {
                col.Spacing(_isThermal ? 3 : 5);
                ComposeHeader(col);
                ComposeInventoryTable(col);
                ComposeFooter(col);
            });
        });
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        col.Item().AlignCenter().Text(_branchName).FontSize(_isThermal ? 11 : 14).Bold().FontColor(Colors.Black);
        col.Item().AlignCenter().Text(_reportTitle).FontSize(_isThermal ? 12 : 18).Bold().FontColor(Colors.Black);
        col.Item().AlignCenter().Text(_isEnglish ? $"Date: {_reportDate:dd/MM/yyyy}" : $"التاريخ: {_reportDate:dd/MM/yyyy}").FontSize(_isThermal ? 7 : 10).FontColor(Colors.Grey.Darken2);
        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
    }

    private void ComposeInventoryTable(ColumnDescriptor col)
    {
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(1.2f); // ID
                cd.RelativeColumn(4);    // Name
                cd.RelativeColumn(2);    // Category
                cd.RelativeColumn(1.8f); // Unit
                cd.RelativeColumn(2);    // Stock
                cd.RelativeColumn(2);    // Min
            });

            t.Header(h =>
            {
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "ID" : "الكود").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Item" : "اسم الصنف").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Cat" : "الفئة").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Unit" : "الوحدة").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Stock" : "الرصيد").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Min" : "الحد الأدنى").Bold();
            });

            foreach (var itm in _inventory)
            {
                bool isLow = itm.TrackInventory && itm.CurrentQuantity <= itm.MinimumQuantity;
                string itemName = _isEnglish ? (itm.ItemNameEn ?? itm.ItemNameAr) : (itm.ItemNameAr ?? itm.ItemNameEn);
                string catName = _isEnglish ? (itm.CategoryNameEn ?? itm.CategoryNameAr) : (itm.CategoryNameAr ?? itm.CategoryNameEn);
                string unitName = _isEnglish ? (itm.UnitNameEn ?? itm.UnitNameAr) : (itm.UnitNameAr ?? itm.UnitNameEn);

                t.Cell().Border(1).Padding(1).AlignCenter().Text(itm.Id.ToString()).FontSize(_isThermal ? 7 : 9);
                t.Cell().Border(1).Padding(1).AlignRight().Text(itemName ?? "-").FontSize(_isThermal ? 7 : 9).Bold();
                t.Cell().Border(1).Padding(1).AlignCenter().Text(catName ?? "-").FontSize(_isThermal ? 7 : 8);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(unitName ?? "-").FontSize(_isThermal ? 7 : 8);
                
                var qtyCell = t.Cell().Border(1).Padding(1).AlignCenter();
                if (isLow)
                    qtyCell.Text(itm.CurrentQuantity.ToString("0.###")).Bold().FontColor(Colors.Red.Medium).FontSize(_isThermal ? 7 : 9);
                else
                    qtyCell.Text(itm.CurrentQuantity.ToString("0.###")).FontSize(_isThermal ? 7 : 9).Bold();

                t.Cell().Border(1).Padding(1).AlignCenter().Text(itm.MinimumQuantity.ToString("0.###")).FontSize(_isThermal ? 7 : 9);
            }
        });
    }

    private void ComposeFooter(ColumnDescriptor col)
    {
        col.Item().PaddingTop(10).AlignCenter().Text(text =>
        {
            text.Span(_isEnglish ? "Printed at: " : "طبع في: ").FontSize(7);
            text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).FontSize(7);
        });
    }
}
