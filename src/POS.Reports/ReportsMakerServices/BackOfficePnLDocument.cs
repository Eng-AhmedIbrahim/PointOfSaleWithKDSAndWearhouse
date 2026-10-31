using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System;
using POS.Contract.Dtos.ReportingDtos;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficePnLDocument : IDocument
{
    private readonly SalesSummaryDto _summary;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficePnLDocument(SalesSummaryDto summary, string branchName, DateTime from, DateTime to, bool isThermal = false, string lang = "ar")
    {
        _summary = summary ?? new SalesSummaryDto();
        _branchName = branchName;
        _fromDate = from;
        _toDate = to;
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
                col.Spacing(_isThermal ? 3 : 6);
                ComposeHeader(col);
                ComposePnLSummary(col);
                ComposeDetailedExpenses(col);
                ComposeFooter(col);
            });
        });
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        col.Item().AlignCenter().Text(_branchName).FontSize(_isThermal ? 11 : 14).Bold().FontColor(Colors.Black);
        col.Item().AlignCenter().Text(_isEnglish ? "Profit & Loss (P&L) Report" : "تقرير الأرباح والخسائر").FontSize(_isThermal ? 12 : 18).Bold().FontColor(Colors.Black);
        
        string dateStr = _fromDate.Date == _toDate.Date 
            ? _fromDate.ToString("dd/MM/yyyy") 
            : $"{_fromDate:dd/MM/yyyy} - {_toDate:dd/MM/yyyy}";
        col.Item().AlignCenter().Text(_isEnglish ? $"Period: {dateStr}" : $"الفترة: {dateStr}").FontSize(_isThermal ? 7 : 10).FontColor(Colors.Grey.Darken2);
        
        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
    }

    private void ComposePnLSummary(ColumnDescriptor col)
    {
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(2); });

            // Income
            t.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten4).Padding(2).Text(_isEnglish ? "1. REVENUE (INCOME)" : "1. الإيرادات").Bold();
            
            AddPnLRow(t, _isEnglish ? "Gross Sales" : "إجمالي المبيعات", _summary.Overall.TotalSales);
            AddPnLRow(t, _isEnglish ? "Discounts" : "الخصومات", -_summary.Overall.TotalDiscount, Colors.Red.Medium);
            AddPnLRow(t, _isEnglish ? "Net Sales" : "صافي المبيعات", _summary.Overall.TotalSales - _summary.Overall.TotalDiscount, Colors.Black, true);

            // Expenses
            t.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten4).Padding(2).Text(_isEnglish ? "2. OPERATING EXPENSES" : "2. المصروفات التشغيلية").Bold();
            
            var totalExpenses = _summary.DetailedExpenses?.Sum(e => e.Amount) ?? 0;
            if (_summary.DetailedExpenses != null)
            {
                var grouped = _summary.DetailedExpenses.GroupBy(e => e.Category).Select(g => new { Cat = g.Key, Amount = g.Sum(x => x.Amount) });
                foreach(var g in grouped)
                    AddPnLRow(t, g.Cat, -g.Amount, Colors.Red.Medium);
            }
            
            AddPnLRow(t, _isEnglish ? "Total Expenses" : "إجمالي المصروفات", -totalExpenses, Colors.Red.Medium, true);

            // Final Profit
            decimal netProfit = (_summary.Overall.TotalSales - _summary.Overall.TotalDiscount) - totalExpenses;
            
            t.Cell().Border(1).Background(Colors.Black).Padding(3).Text(_isEnglish ? "NET PROFIT / LOSS" : "صافي الربح / الخسارة").Bold().FontColor(Colors.White).FontSize(_isThermal ? 9 : 12);
            t.Cell().Border(1).Background(Colors.Black).Padding(3).AlignCenter().Text(netProfit.ToString("0.##")).Bold().FontColor(Colors.White).FontSize(_isThermal ? 9 : 12);
        });
    }

    private void AddPnLRow(TableDescriptor t, string label, decimal value, string? hexColor = null, bool isBold = false)
    {
        t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(label).Style(isBold ? TextStyle.Default.Bold() : TextStyle.Default);
        t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2).AlignCenter().Text(value.ToString("N2")).FontColor(hexColor ?? Colors.Black).Style(isBold ? TextStyle.Default.Bold() : TextStyle.Default);
    }

    private void ComposeDetailedExpenses(ColumnDescriptor col)
    {
        if (_summary.DetailedExpenses == null || !_summary.DetailedExpenses.Any()) return;
        if (_isThermal) return; // Keep thermal P&L short

        col.Item().PageBreak();
        col.Item().PaddingTop(10).AlignCenter().Text(_isEnglish ? "Detailed Payouts / Expenses Log" : "سجل المصروفات والمدفوعات التفصيلي").FontSize(14).Bold();
        
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd => { 
                cd.RelativeColumn(1.5f); // Date
                cd.RelativeColumn(2);    // Category
                cd.RelativeColumn(3);    // Authorizer (Cashier)
                cd.RelativeColumn(3.5f); // Description
                cd.RelativeColumn(2);    // Amount
            });

            t.Header(h => {
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Date" : "التاريخ").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Category" : "الفئة").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Authorized By" : "المسئول (الكاشير)").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Description" : "البيان").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Amount" : "المبلغ").Bold();
            });

            foreach (var e in _summary.DetailedExpenses)
            {
                t.Cell().Border(1).Padding(1).AlignCenter().Text(e.Date.ToString("dd/MM HH:mm"));
                t.Cell().Border(1).Padding(1).AlignCenter().Text(e.Category);
                t.Cell().Border(1).Padding(1).AlignCenter().Text($"{e.CreatedByName} (ID: {e.CreatedById})").FontSize(8);
                t.Cell().Border(1).Padding(1).AlignRight().Text(e.Description).FontSize(9);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(e.Amount.ToString("0.##")).Bold();
            }
        });
    }

    private void ComposeFooter(ColumnDescriptor col)
    {
        col.Item().PaddingTop(15).AlignCenter().Text(text =>
        {
            text.Span(_isEnglish ? "Printed at: " : "طبع في: ").FontSize(7);
            text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).FontSize(7);
        });
    }
}
