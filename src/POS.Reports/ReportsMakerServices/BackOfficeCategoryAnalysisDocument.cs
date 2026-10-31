using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using POS.Contract.Dtos.ReportingDtos;
using POS.Contract.Dtos.OrderDtos;
using System.Collections.Generic;
using System.Linq;
using System;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficeCategoryAnalysisDocument : IDocument
{
    private readonly List<SalesItemSummaryDto> _items;
    private readonly List<POS.Contract.Dtos.OrderDtos.OrderDto> _orders;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficeCategoryAnalysisDocument(List<SalesItemSummaryDto> items, List<OrderDto> orders, string branchName, DateTime fromDate, DateTime toDate, bool isThermal = false, string lang = "ar")
    {
        _items = items ?? new List<SalesItemSummaryDto>();
        _orders = orders ?? new List<POS.Contract.Dtos.OrderDtos.OrderDto>();
        _branchName = branchName;
        _fromDate = fromDate;
        _toDate = toDate;
        _isThermal = isThermal;
        _isEnglish = (lang ?? "ar").ToLower() == "en";
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
                col.Spacing(_isThermal ? 2 : 5);
                ComposeHeader(col);
                ComposeCategoryTable(col);
                ComposeFooter(col);
            });
        });
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        // Branch Name (Black, Center)
        col.Item().AlignCenter().Text(_branchName).FontSize(_isThermal ? 11 : 14).Bold().FontColor(Colors.Black);
        
        // Report Title (Black, Center)
        col.Item().AlignCenter().Text(_isEnglish ? "Category Analysis Report" : "تحليل المبيعات حسب الفئة").FontSize(_isThermal ? 12 : 18).Bold().FontColor(Colors.Black);
        
        // Period
        col.Item().AlignCenter().Text(_isEnglish ? $"Period: {_fromDate:dd/MM/yyyy} - {_toDate:dd/MM/yyyy}" : $"الفترة من: {_fromDate:dd/MM/yyyy} إلى: {_toDate:dd/MM/yyyy}").FontSize(_isThermal ? 7 : 10).FontColor(Colors.Grey.Darken2);
        
        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
    }

    private void ComposeCategoryTable(ColumnDescriptor col)
    {
        var categoryData = _items.GroupBy(i => i.CategoryName)
            .Select(g => new
            {
                CategoryName = g.Key,
                Total = g.Sum(x => x.TotalAmount),
                OrderCount = CalculateOrderCount(g.Key)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        decimal grandTotal = categoryData.Sum(x => x.Total);

        col.Item().PaddingTop(5).Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(3); // Category Name
                cd.RelativeColumn(1.8f); // Order Count
                cd.RelativeColumn(2.5f); // Total
                cd.RelativeColumn(1.5f); // Percentage
            });

            t.Header(h =>
            {
                h.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(1).AlignCenter().Text(_isEnglish ? "Category" : "الفئة").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(1).AlignCenter().Text(_isEnglish ? "Orders" : "الطلبات").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(1).AlignCenter().Text(_isEnglish ? "Total" : "الإجمالي").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(1).AlignCenter().Text(_isEnglish ? "%" : "%").Bold();
            });

            foreach (var cat in categoryData)
            {
                t.Cell().Border(1).Padding(1).AlignCenter().Text(cat.CategoryName).Bold();
                t.Cell().Border(1).Padding(1).AlignCenter().Text(cat.OrderCount.ToString());
                t.Cell().Border(1).Padding(1).AlignCenter().Text(cat.Total.ToString("0.##"));
                t.Cell().Border(1).Padding(1).AlignCenter().Text(grandTotal > 0 ? (cat.Total / grandTotal * 100).ToString("N1") + "%" : "0%");
            }

            // Total Row
            t.Cell().ColumnSpan(2).Border(1).Background(Colors.Grey.Lighten2).Padding(2).AlignCenter().Text(_isEnglish ? "Grand Total" : "الإجمالي الكلي").Bold();
            t.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text(grandTotal.ToString("0.##")).Bold();
            t.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text("100%").Bold();
        });
    }

    private int CalculateOrderCount(string categoryName)
    {
        if (!_orders.Any()) return 0;
        return _orders.Count(o => o.OrderDetails?.Any(d => (d.CategoryName ?? "غير محدد") == categoryName && d.IsVoided != true) ?? false);
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
