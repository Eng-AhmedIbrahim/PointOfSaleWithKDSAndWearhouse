using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using POS.Contract.Dtos.OrderDtos;
using System.Collections.Generic;
using System.Linq;
using System;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficeSalesDetailedDocument : IDocument
{
    private readonly List<OrderDto> _orders;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly string _lang;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficeSalesDetailedDocument(List<OrderDto> orders, string branchName, DateTime fromDate, DateTime toDate, string lang = "ar", bool isThermal = false)
    {
        _orders = orders ?? new List<OrderDto>();
        _branchName = branchName;
        _fromDate = fromDate;
        _toDate = toDate;
        _lang = (lang ?? "ar").ToLower();
        _isEnglish = _lang == "en";
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
                col.Spacing(_isThermal ? 3 : 6);
                ComposeHeader(col);
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
        col.Item().AlignCenter().Text(_isEnglish ? "Detailed Sales Report" : "تقرير المبيعات التفصيلية").FontSize(_isThermal ? 12 : 18).Bold().FontColor(Colors.Black);
        
        // Period
        col.Item().AlignCenter().Text(_isEnglish ? $"Period: {_fromDate:dd/MM/yyyy} - {_toDate:dd/MM/yyyy}" : $"الفترة من: {_fromDate:dd/MM/yyyy} إلى: {_toDate:dd/MM/yyyy}").FontSize(_isThermal ? 7 : 10).FontColor(Colors.Grey.Darken2);
        
        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
    }

    private void ComposeOrdersList(ColumnDescriptor col)
    {
        foreach (var order in _orders)
        {
            bool isVoided = order.OrderState == "Voided";
            decimal orderVoid = order.VoidAmount ?? (isVoided ? (order.GrandTotal ?? 0) : 0);
            decimal orderDiscount = order.TotalDiscount ?? (order.DiscountedItems ?? 0);

            col.Item().PaddingTop(10).Column(oCol =>
            {
                // Order Info as a Table (for consistency)
                oCol.Item().Table(t => {
                    t.ColumnsDefinition(cd => {
                        cd.RelativeColumn(3); // # ID (Type)
                        cd.RelativeColumn(4); // Date/Time
                        cd.RelativeColumn(3); // Total
                    });

                    string typeBadge = $"#{order.OrderId} ({order.OrderType})";
                    if (isVoided)
                    {
                        typeBadge += _isEnglish ? " [VOIDED]" : " [ملغي]";
                    }
                    t.Cell().Border(1).Background(isVoided ? Colors.Red.Lighten5 : Colors.Grey.Lighten4).Padding(2).AlignCenter().Text(typeBadge).Bold().FontColor(isVoided ? Colors.Red.Darken2 : Colors.Black);
                    
                    // Format Date & Time nicely: e.g. 18/10/2026 09:43 PM
                    string dateTimeStr = order.OrderDate?.ToString("dd/MM/yyyy  hh:mm tt") ?? "-";
                    t.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text(dateTimeStr).FontSize(8);
                    
                    var totalText = order.GrandTotal?.ToString("0.##");
                    if (isVoided)
                    {
                        t.Cell().Border(1).Background(Colors.Red.Lighten5).Padding(2).AlignCenter().Text(totalText).Bold().FontColor(Colors.Red.Darken2).Strikethrough();
                    }
                    else
                    {
                        t.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text(totalText).Bold();
                    }
                });

                // Additional Info (Responsible, Customer, etc.)
                oCol.Item().Table(it => {
                    it.ColumnsDefinition(icd => {
                        icd.RelativeColumn(1);
                        icd.RelativeColumn(3);
                    });

                    if (!string.IsNullOrEmpty(order.CashierName))
                    {
                        it.Cell().Border(1).Padding(2).Text(_isEnglish ? "Cashier:" : "الكاشير:").Bold().FontSize(8);
                        it.Cell().Border(1).Padding(2).Text(order.CashierName).Bold().FontSize(9);
                    }

                    string customerName = order.CustomerName ?? order.TakeAwayCustomerName ?? "-";
                    string phone = order.CustomerPhone ?? order.TakeawayCustomerPhone ?? order.Phone1 ?? "";
                    
                    if (customerName != "-" || !string.IsNullOrEmpty(phone))
                    {
                        string displayCustomer = customerName == "-" ? phone : (string.IsNullOrEmpty(phone) ? customerName : $"{customerName} - {phone}");
                        
                        it.Cell().Border(1).Padding(2).Text(_isEnglish ? "Customer:" : "العميل:").Bold().FontSize(8);
                        it.Cell().Border(1).Padding(2).Text(displayCustomer).Bold().FontSize(9);
                    }

                    // Void Information
                    if (isVoided || orderVoid > 0)
                    {
                        it.Cell().Border(1).Background(Colors.Red.Lighten5).Padding(2).Text(_isEnglish ? "Void Details:" : "بيانات الإلغاء:").Bold().FontSize(8).FontColor(Colors.Red.Darken2);
                        string voidText = $"{(_isEnglish ? "Amount: " : "القيمة: ")}{orderVoid:0.##}";
                        if (!string.IsNullOrEmpty(order.VoidReason))
                            voidText += $" | {(_isEnglish ? "Reason: " : "السبب: ")}{order.VoidReason}";
                        if (!string.IsNullOrEmpty(order.VoidByName))
                            voidText += $" | {(_isEnglish ? "By: " : "بواسطة: ")}{order.VoidByName}";
                        it.Cell().Border(1).Background(Colors.Red.Lighten5).Padding(2).Text(voidText).FontSize(8).FontColor(Colors.Red.Darken2).Bold();
                    }

                    // Discount Information
                    if (orderDiscount > 0)
                    {
                        it.Cell().Border(1).Background(Colors.Green.Lighten5).Padding(2).Text(_isEnglish ? "Discount Details:" : "بيانات الخصم:").Bold().FontSize(8).FontColor(Colors.Green.Darken3);
                        string discText = $"{(_isEnglish ? "Amount: " : "القيمة: ")}{orderDiscount:0.##}";
                        if (!string.IsNullOrEmpty(order.DiscountReason))
                            discText += $" | {(_isEnglish ? "Reason: " : "السبب: ")}{order.DiscountReason}";
                        if (!string.IsNullOrEmpty(order.DiscountByName))
                            discText += $" | {(_isEnglish ? "By: " : "بواسطة: ")}{order.DiscountByName}";
                        it.Cell().Border(1).Background(Colors.Green.Lighten5).Padding(2).Text(discText).FontSize(8).FontColor(Colors.Green.Darken3).Bold();
                    }
                });

                if (order.OrderDetails != null && order.OrderDetails.Any())
                {
                    oCol.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(5);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(3);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Item" : "الصنف").Bold().FontSize(8);
                            h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Qty" : "عدد").Bold().FontSize(8);
                            h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Total" : "الإجمالي").Bold().FontSize(8);
                        });

                        foreach (var itm in order.OrderDetails)
                        {
                            t.Cell().Border(1).Padding(1).AlignRight().Text(_isEnglish ? (itm.Name ?? itm.NameAr) : (itm.NameAr ?? itm.Name)).FontSize(8);
                            t.Cell().Border(1).Padding(1).AlignCenter().Text(itm.Quantity.ToString("G29")).FontSize(8);
                            t.Cell().Border(1).Padding(1).AlignCenter().Text(itm.Total?.ToString("0.##")).FontSize(8);
                        }
                    });
                }
                oCol.Item().PaddingVertical(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten4);
            });
        }
    }

    private void ComposeFooter(ColumnDescriptor col)
    {
        col.Item().PaddingTop(20).AlignCenter().Text(text =>
        {
            text.Span(_isEnglish ? "Printed at: " : "طبع في: ").FontSize(8);
            text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).FontSize(8);
        });
    }
}
