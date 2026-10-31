using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using POS.Contract.Dtos.OrderDtos;
using System.Collections.Generic;
using System.Linq;
using System;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficeDiscountReportDocument : IDocument
{
    private readonly List<POS.Contract.Dtos.OrderDtos.OrderDto> _orders;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;
    private readonly string _reportTitle;

    public BackOfficeDiscountReportDocument(List<OrderDto> orders, string branchName, DateTime fromDate, DateTime toDate, string reportTitle = "تقرير الخصومات", string lang = "ar", bool isThermal = false)
    {
        _orders = orders ?? new List<OrderDto>();
        _branchName = branchName;
        _fromDate = fromDate;
        _toDate = toDate;
        _reportTitle = reportTitle;
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
                col.Spacing(_isThermal ? 3 : 5);
                ComposeHeader(col);
                ComposeDiscountSummary(col);
                ComposeOrdersList(col);
                ComposeFooter(col);
            });
        });
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        // Branch Name (Black, Center)
        col.Item().AlignCenter().Text(_branchName).FontSize(_isThermal ? 11 : 14).Bold().FontColor(Colors.Black);
        
        // Report Title (Black, Center)
        col.Item().AlignCenter().Text(_isEnglish ? "Discounted Orders Report" : "تقرير الخصومات").FontSize(_isThermal ? 12 : 18).Bold().FontColor(Colors.Black);
        
        // Period
        col.Item().AlignCenter().Text(_isEnglish ? $"Period: {_fromDate:dd/MM/yyyy} - {_toDate:dd/MM/yyyy}" : $"الفترة من: {_fromDate:dd/MM/yyyy} إلى: {_toDate:dd/MM/yyyy}").FontSize(_isThermal ? 7 : 10).FontColor(Colors.Grey.Darken2);
        
        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
    }

    private decimal GetOrderDiscount(OrderDto o)
        => (o.TotalOrderDiscount ?? 0) + (o.TotalDiscount ?? 0) + (o.DiscountedItems ?? 0);

    private void ComposeDiscountSummary(ColumnDescriptor col)
    {
        var totalDiscount = _orders.Sum(o => GetOrderDiscount(o));
        var orderCount = _orders.Count;
        
        col.Item().PaddingVertical(_isThermal ? 2 : 5).Background(Colors.Grey.Lighten4).Padding(_isThermal ? 3 : 8).Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text(_isEnglish ? "Total Discount" : "إجمالي الخصم").FontSize(_isThermal ? 7 : 9).Bold();
                c.Item().Text(totalDiscount.ToString("N2")).FontSize(_isThermal ? 10 : 12).Bold().FontColor(Colors.Black);
            });
            row.RelativeItem().Column(c =>
            {
                c.Item().Text(_isEnglish ? "Orders Count" : "عدد الطلبات").FontSize(_isThermal ? 7 : 9).Bold();
                c.Item().Text(orderCount.ToString()).FontSize(_isThermal ? 10 : 12).Bold();
            });
        });
    }

    private void ComposeOrdersList(ColumnDescriptor col)
    {
        col.Item().PaddingTop(5).Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(1.5f); // # ID
                cd.RelativeColumn(3);    // Type/Responsible
                cd.RelativeColumn(1.5f); // Total
                cd.RelativeColumn(1.5f); // Disc
                cd.RelativeColumn(2.5f); // Reason
            });

            t.Header(h =>
            {
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "#" : "الرقم").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Type" : "النوع").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Total" : "الإجمالي").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Disc" : "خصم").Bold().FontColor(Colors.Black);
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Reason" : "السبب").Bold();
            });

            foreach (var order in _orders)
            {
                var disc = GetOrderDiscount(order);
                t.Cell().Border(1).Padding(1).AlignCenter().Text($"#{order.OrderId}").FontSize(_isThermal ? 7 : 9);
                t.Cell().Border(1).Padding(1).AlignCenter().Text($"{order.OrderType}").FontSize(_isThermal ? 7 : 9);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(order.GrandTotal?.ToString("0.##")).FontSize(_isThermal ? 7 : 9);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(disc.ToString("0.##")).Bold().FontSize(_isThermal ? 7 : 9);
                t.Cell().Border(1).Padding(1).AlignRight().Text(order.DiscountReason ?? "-").FontSize(_isThermal ? 7 : 8);
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
