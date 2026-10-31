using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using POS.Contract.Dtos.OrderDtos;
using System.Collections.Generic;
using System.Linq;
using System;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficeVoidReportDocument : IDocument
{
    private readonly List<POS.Contract.Dtos.OrderDtos.OrderDto> _orders;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly string _reportTitle;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficeVoidReportDocument(List<POS.Contract.Dtos.OrderDtos.OrderDto> orders, string branchName, DateTime fromDate, DateTime toDate, string title = "تقرير الإلغاءات التفصيلي", string lang = "ar", bool isThermal = false)
    {
        _orders = orders ?? new List<POS.Contract.Dtos.OrderDtos.OrderDto>();
        _branchName = branchName;
        _fromDate = fromDate;
        _toDate = toDate;
        _reportTitle = title;
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
                ComposeVoidSummary(col);
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
        col.Item().AlignCenter().Text(_reportTitle).FontSize(_isThermal ? 12 : 18).Bold().FontColor(Colors.Black);
        
        // Period
        col.Item().AlignCenter().Text(_isEnglish ? $"Period: {_fromDate:dd/MM/yyyy} - {_toDate:dd/MM/yyyy}" : $"الفترة من: {_fromDate:dd/MM/yyyy} إلى: {_toDate:dd/MM/yyyy}").FontSize(_isThermal ? 7 : 10).FontColor(Colors.Grey.Darken2);
        
        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
    }

    private void ComposeVoidSummary(ColumnDescriptor col)
    {
        // Group by OrderId to avoid double-counting duplicates
        var uniqueOrders = _orders.GroupBy(o => o.OrderId).Select(g => new { 
            Order = g.First(), 
            TotalVoid = g.Sum(o => o.VoidAmount ?? 0) > 0 ? g.Sum(o => o.VoidAmount ?? 0) : (g.First().OrderState == "Voided" ? g.First().GrandTotal : 0) 
        }).ToList();

        var totalVoided = uniqueOrders.Sum(x => x.TotalVoid ?? 0);
        var voidCount = uniqueOrders.Count;
        
        col.Item().PaddingVertical(_isThermal ? 2 : 5).Background(Colors.Grey.Lighten4).Padding(_isThermal ? 3 : 8).Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text(_isEnglish ? "Total Voided" : "إجمالي الإلغاء").FontSize(_isThermal ? 7 : 9).Bold();
                c.Item().Text(totalVoided.ToString("0.##")).FontSize(_isThermal ? 10 : 12).Bold().FontColor(Colors.Black);
            });
            row.RelativeItem().Column(c =>
            {
                c.Item().Text(_isEnglish ? "Voided Orders" : "الطلبات الملغاة").FontSize(_isThermal ? 7 : 9).Bold();
                c.Item().Text(voidCount.ToString()).FontSize(_isThermal ? 10 : 12).Bold();
            });
        });
    }

    private void ComposeOrdersList(ColumnDescriptor col)
    {
        var uniqueOrderIds = _orders.Select(o => o.OrderId).Distinct().ToList();

        foreach (var orderId in uniqueOrderIds)
        {
            var group = _orders.Where(o => o.OrderId == orderId).ToList();
            var mainOrder = group.First();

            var sumVoidAmount = group.Sum(o => o.VoidAmount ?? 0);
            if (sumVoidAmount == 0 && mainOrder.OrderState == "Voided") 
                sumVoidAmount = mainOrder.GrandTotal ?? 0;

            var users = group.Select(g => g.VoidByName ?? g.CashierName).Where(u => !string.IsNullOrEmpty(u)).Distinct();
            var reasons = group.Select(g => g.VoidReason).Where(r => !string.IsNullOrEmpty(r)).Distinct();

            col.Item().PaddingTop(8).Column(oCol =>
            {
                // Header Table for the Order
                oCol.Item().Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(1.5f); // Order #
                        cd.RelativeColumn(2); // State
                        cd.RelativeColumn(2.5f); // User
                        cd.RelativeColumn(3); // Reason
                        cd.RelativeColumn(2); // Amount Voided
                    });

                    t.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text($"#{mainOrder.OrderId}").Bold().FontSize(_isThermal ? 8 : 10);
                    
                    var stateText = mainOrder.OrderState == "Voided" ? (_isEnglish ? "Fully Voided" : "إلغاء كامل") : (_isEnglish ? "Partial Void" : "إلغاء جزئي");
                    t.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text(stateText).Bold().FontSize(_isThermal ? 8 : 10).FontColor(Colors.Red.Darken2);
                    
                    t.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text(string.Join(", ", users)).FontSize(_isThermal ? 8 : 10);
                    t.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text(string.Join(" / ", reasons)).FontSize(_isThermal ? 8 : 10);
                    t.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text(sumVoidAmount.ToString("0.##")).Bold().FontSize(_isThermal ? 8 : 10);
                });

                var voidedItems = mainOrder.OrderDetails?.Where(i => i.IsVoided || mainOrder.OrderState == "Voided").ToList() ?? new List<POS.Contract.Models.TableItem>();

                if (voidedItems.Any())
                {
                    oCol.Item().Table(it =>
                    {
                        it.ColumnsDefinition(icd =>
                        {
                            icd.RelativeColumn(4); // Item Name
                            icd.RelativeColumn(1.5f); // Qty
                            icd.RelativeColumn(2); // Price
                            icd.RelativeColumn(2); // Cost Before (Total)
                            icd.RelativeColumn(2); // Cost After
                        });

                        it.Header(h =>
                        {
                            h.Cell().Border(1).Background(Colors.Grey.Lighten5).Padding(2).AlignCenter().Text(_isEnglish ? "Item" : "الصنف المُلغى").Bold().FontSize(_isThermal ? 7 : 9);
                            h.Cell().Border(1).Background(Colors.Grey.Lighten5).Padding(2).AlignCenter().Text(_isEnglish ? "Qty" : "الكمية").Bold().FontSize(_isThermal ? 7 : 9);
                            h.Cell().Border(1).Background(Colors.Grey.Lighten5).Padding(2).AlignCenter().Text(_isEnglish ? "Price" : "السعر").Bold().FontSize(_isThermal ? 7 : 9);
                            h.Cell().Border(1).Background(Colors.Grey.Lighten5).Padding(2).AlignCenter().Text(_isEnglish ? "Before" : "التكلفة قبل").Bold().FontSize(_isThermal ? 7 : 9);
                            h.Cell().Border(1).Background(Colors.Grey.Lighten5).Padding(2).AlignCenter().Text(_isEnglish ? "After" : "التكلفة بعد").Bold().FontSize(_isThermal ? 7 : 9);
                        });

                        foreach (var item in voidedItems)
                        {
                            it.Cell().Border(1).Padding(2).AlignRight().Text(_isEnglish ? (item.Name ?? item.NameAr) : (item.NameAr ?? item.Name)).FontSize(_isThermal ? 7 : 9);
                            it.Cell().Border(1).Padding(2).AlignCenter().Text(item.Quantity.ToString("G29")).FontSize(_isThermal ? 7 : 9);
                            it.Cell().Border(1).Padding(2).AlignCenter().Text(item.Price?.ToString("0.##")).FontSize(_isThermal ? 7 : 9);
                            
                            var costBefore = item.Quantity * (item.Price ?? 0);
                            it.Cell().Border(1).Padding(2).AlignCenter().Text(costBefore.ToString("0.##")).FontSize(_isThermal ? 7 : 9).FontColor(Colors.Grey.Darken2);
                            
                            var costAfter = item.IsVoided || mainOrder.OrderState == "Voided" ? 0 : costBefore;
                            it.Cell().Border(1).Padding(2).AlignCenter().Text(costAfter.ToString("0.##")).FontSize(_isThermal ? 7 : 9).FontColor(Colors.Black);
                        }
                    });
                }
                else
                {
                    oCol.Item().Border(1).Padding(2).AlignCenter().Text(_isEnglish ? "No detailed items found." : "لا توجد تفاصيل أصناف.").FontSize(_isThermal ? 7 : 9).FontColor(Colors.Grey.Darken1);
                }
            });
        }
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
