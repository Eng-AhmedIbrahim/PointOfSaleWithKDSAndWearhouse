using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficeLowStockReportDocument : IDocument
{
    private readonly IEnumerable<dynamic> _inventory;
    private readonly string _branchName;
    private readonly DateTime _reportDate;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficeLowStockReportDocument(IEnumerable<dynamic> inventory, string branchName, DateTime reportDate, string lang = "ar", bool isThermal = false)
    {
        _inventory = inventory ?? new List<dynamic>();
        _branchName = branchName;
        _reportDate = reportDate;
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
        col.Item().AlignCenter().Text(_isEnglish ? "Low Stock Alert Report" : "تقرير نواقص المخزن").FontSize(_isThermal ? 12 : 18).Bold().FontColor(Colors.Black);
        col.Item().AlignCenter().Text(_isEnglish ? $"Date: {_reportDate:yyyy/MM/dd}" : $"تاريخ اليوم: {_reportDate:yyyy/MM/dd}").FontSize(_isThermal ? 7 : 10).FontColor(Colors.Grey.Darken3);
        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
        col.Item().PaddingBottom(5).AlignCenter().Text(_isEnglish ? "Showing items reached or below Minimum Quantity" : "الأصناف التي وصلت أو تجاوزت الحد الأدنى المسموح به").FontSize(_isThermal ? 6 : 9).Italic();
    }

    private void ComposeInventoryTable(ColumnDescriptor col)
    {
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(4); // Item
                cd.RelativeColumn(1.5f); // Current
                cd.RelativeColumn(1.5f); // Min
                cd.RelativeColumn(1); // Unit
            });

            t.Header(h =>
            {
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Item" : "الصنف").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Cur" : "الحالي").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Min" : "الحد").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Unit" : "وحدة").Bold();
            });

            foreach (var inv in _inventory)
            {
                t.Cell().Border(1).Padding(1).AlignRight().Text((string)inv.ItemName).FontSize(_isThermal ? 7 : 9);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(((decimal)inv.CurrentQty).ToString("0.##")).Bold().FontColor(Colors.Red.Medium);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(((decimal)inv.MinQty).ToString("0.##")).FontSize(_isThermal ? 7 : 9);
                t.Cell().Border(1).Padding(1).AlignCenter().Text((string)inv.Unit).FontSize(_isThermal ? 7 : 9);
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
