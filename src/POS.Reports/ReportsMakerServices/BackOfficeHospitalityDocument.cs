using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using POS.Contract.Dtos.OrderDtos;
using System.Collections.Generic;
using System.Linq;
using System;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficeHospitalityDocument : IDocument
{
    private readonly List<OrderDto> _orders;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficeHospitalityDocument(List<OrderDto> orders, string branchName, DateTime fromDate, DateTime toDate, bool isThermal = false, string lang = "ar")
    {
        _orders = orders ?? new List<OrderDto>();
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
                col.Spacing(_isThermal ? 3 : 5);
                ComposeHeader(col);
                ComposeSummary(col);
                ComposeTable(col);
                ComposeFooter(col);
            });
        });
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        // Branch Name (Black, Center)
        col.Item().AlignCenter().Text(_branchName).FontSize(_isThermal ? 11 : 14).Bold().FontColor(Colors.Black);
        
        // Main Title (Black, Center)
        col.Item().AlignCenter().Text(_isEnglish ? "Hospitality Report" : "تقرير الضيافة").FontSize(_isThermal ? 12 : 18).Bold().FontColor(Colors.Black);
        
        // Period
        col.Item().AlignCenter().Text(_isEnglish ? $"Period: {_fromDate:dd/MM/yyyy} - {_toDate:dd/MM/yyyy}" : $"الفترة من: {_fromDate:dd/MM/yyyy} إلى: {_toDate:dd/MM/yyyy}").FontSize(_isThermal ? 7 : 10).FontColor(Colors.Grey.Darken2);
        
        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
    }

    private void ComposeSummary(ColumnDescriptor col)
    {
        var totalOrders = _orders.Count;
        var totalAmount = _orders.Sum(o => o.TotalVoid ?? o.GrandTotal ?? 0);

        col.Item().PaddingVertical(_isThermal ? 2 : 5).Background(Colors.Grey.Lighten4).Padding(_isThermal ? 3 : 8).Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text(_isEnglish ? "Total Orders" : "عدد الطلبات").FontSize(_isThermal ? 7 : 9).Bold();
                c.Item().Text(totalOrders.ToString()).FontSize(_isThermal ? 10 : 12).Bold();
            });
            row.RelativeItem().Column(c =>
            {
                c.Item().Text(_isEnglish ? "Total Amount" : "إجمالي القيمة").FontSize(_isThermal ? 7 : 9).Bold();
                c.Item().Text(totalAmount.ToString("0.##")).FontSize(_isThermal ? 10 : 12).Bold().FontColor(Colors.Black);
            });
        });
    }

    private void ComposeTable(ColumnDescriptor col)
    {
        col.Item().PaddingTop(2).Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(1.2f); // ID
                cd.RelativeColumn(3);    // Responsible
                cd.RelativeColumn(3.5f); // Reason
                cd.RelativeColumn(2);    // Amount
                cd.RelativeColumn(1.8f); // Time
            });

            t.Header(h =>
            {
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(2).AlignCenter().Text(_isEnglish ? "#" : "الرقم").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(2).AlignCenter().Text(_isEnglish ? "Responsible" : "المسئول").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(2).AlignCenter().Text(_isEnglish ? "Reason" : "السبب").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(2).AlignCenter().Text(_isEnglish ? "Amt" : "القيمة").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(2).AlignCenter().Text(_isEnglish ? "Time" : "الوقت").Bold();
            });

            foreach (var order in _orders)
            {
                t.Cell().Border(1).Padding(2).AlignCenter().Text($"#{order.OrderId}").FontSize(_isThermal ? 7 : 9);
                
                string responsibleName = order.HospitalityResponsibleName;
                string reason = order.HospitalityReason;

                // Auto-parse from OrderNotice if specific pattern is found: (ضيافة: Administrator) - السبب: ضيافة IT
                if (!string.IsNullOrEmpty(order.OrderNotice) && order.OrderNotice.Contains("(ضيافة:") && order.OrderNotice.Contains("السبب:"))
                {
                    try
                    {
                        var note = order.OrderNotice;
                        int startResp = note.IndexOf("(ضيافة:") + 7;
                        int endResp = note.IndexOf(")", startResp);
                        if (startResp > 6 && endResp > startResp)
                        {
                            responsibleName = note.Substring(startResp, endResp - startResp).Trim();
                        }
                        
                        int startReason = note.IndexOf("السبب:") + 6;
                        if (startReason > 5)
                        {
                            reason = note.Substring(startReason).Trim();
                        }
                    }
                    catch { /* Fallback to existing logic if parsing fails */ }
                }

                // Fallbacks if parsing didn't find them or they are empty
                if (string.IsNullOrEmpty(responsibleName)) responsibleName = order.CashierName ?? "-";
                if (string.IsNullOrEmpty(reason)) reason = order.CustomerName ?? order.TakeAwayCustomerName ?? "System";

                t.Cell().Border(1).Padding(2).AlignCenter().Text(responsibleName).FontSize(_isThermal ? 7 : 8).Bold();
                t.Cell().Border(1).Padding(2).AlignCenter().Text(reason).FontSize(_isThermal ? 7 : 8);
                
                var amount = order.TotalVoid ?? order.GrandTotal ?? 0;
                t.Cell().Border(1).Padding(2).AlignCenter().Text(amount.ToString("0.##")).FontSize(_isThermal ? 7 : 9).Bold();
                
                t.Cell().Border(1).Padding(2).AlignCenter().Text(order.OrderDate?.ToString("HH:mm") ?? "-").FontSize(_isThermal ? 7 : 9);
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
