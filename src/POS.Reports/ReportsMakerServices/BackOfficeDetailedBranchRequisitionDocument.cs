using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using POS.Core.Entities.Warehouse;
using System.Collections.Generic;
using System.Linq;
using System;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficeDetailedBranchRequisitionDocument : IDocument
{
    private readonly List<BranchRequisition> _requisitions;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly string _reportTitle;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficeDetailedBranchRequisitionDocument(
        List<BranchRequisition> requisitions,
        string branchName,
        DateTime fromDate,
        DateTime toDate,
        string reportTitle = "تقرير تفاصيل وتعديلات طلبيات المخزن",
        string lang = "ar",
        bool isThermal = false)
    {
        _requisitions = requisitions ?? new List<BranchRequisition>();
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

            var baseStyle = TextStyle.Default.FontFamily("Arial", "Noto Sans Arabic").FontSize(_isThermal ? 8 : 9);
            page.DefaultTextStyle(baseStyle);

            if (!_isEnglish)
                page.ContentFromRightToLeft();

            page.Content().Column(col =>
            {
                col.Spacing(_isThermal ? 3 : 6);
                ComposeHeader(col);
                ComposeRequisitionsList(col);
                ComposeFooter(col);
            });
        });
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        col.Item().AlignCenter().Text(_branchName).FontSize(_isThermal ? 11 : 14).Bold().FontColor(Colors.Black);
        col.Item().AlignCenter().Text(_reportTitle).FontSize(_isThermal ? 12 : 16).Bold().FontColor(Colors.Orange.Darken4);
        col.Item().AlignCenter().Text(_isEnglish ? $"Period: {_fromDate:dd/MM/yyyy} - {_toDate:dd/MM/yyyy}" : $"الفترة من: {_fromDate:dd/MM/yyyy} إلى: {_toDate:dd/MM/yyyy}").FontSize(_isThermal ? 7 : 9).FontColor(Colors.Grey.Darken2);
        col.Item().PaddingVertical(3).LineHorizontal(1).LineColor(Colors.Black);
    }

    private string GetStatusText(RequisitionStatus status)
    {
        if (_isEnglish) return status.ToString();
        return status switch
        {
            RequisitionStatus.Draft => "مسودة",
            RequisitionStatus.Submitted => "تم الإرسال",
            RequisitionStatus.UnderReview => "قيد المراجعة",
            RequisitionStatus.ModificationProposed => "تم اقتراح تعديل الكمية",
            RequisitionStatus.Approved => "معتمدة",
            RequisitionStatus.Preparing => "جاري التجهيز",
            RequisitionStatus.InTransit => "تم الشحن / بالطريق",
            RequisitionStatus.Received => "تم الاستلام",
            RequisitionStatus.Rejected => "مرفوضة",
            RequisitionStatus.Cancelled => "ملغاة",
            _ => status.ToString()
        };
    }

    private string GetStatusColor(RequisitionStatus status)
    {
        return status switch
        {
            RequisitionStatus.Received => Colors.Green.Darken2,
            RequisitionStatus.InTransit => Colors.Blue.Darken2,
            RequisitionStatus.Approved => Colors.Teal.Darken2,
            RequisitionStatus.UnderReview or RequisitionStatus.ModificationProposed => Colors.Orange.Darken3,
            RequisitionStatus.Rejected or RequisitionStatus.Cancelled => Colors.Red.Darken2,
            _ => Colors.Grey.Darken2
        };
    }

    private void ComposeRequisitionsList(ColumnDescriptor col)
    {
        if (!_requisitions.Any())
        {
            col.Item().Padding(10).AlignCenter().Text(_isEnglish ? "No requisitions found for this period." : "لا توجد طلبيات مسجلة لهذه الفترة.").FontSize(9).FontColor(Colors.Grey.Darken1);
            return;
        }

        foreach (var req in _requisitions.OrderByDescending(r => r.RequestDate))
        {
            if (_isThermal)
            {
                ComposeThermalCard(col, req);
            }
            else
            {
                ComposeA4Card(col, req);
            }
        }
    }

    private void ComposeA4Card(ColumnDescriptor col, BranchRequisition req)
    {
        col.Item().PaddingTop(6).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.White).Column(card =>
        {
            // 1. Order Header Bar
            card.Item().Background(Colors.Grey.Lighten3).Padding(4).Row(row =>
            {
                row.ConstantItem(120).Text(req.RequisitionNumber).Bold().FontSize(10).FontColor(Colors.Blue.Darken3);
                row.RelativeItem().Text($"{(_isEnglish ? "Date: " : "تاريخ الطلب: ")}{req.RequestDate:yyyy-MM-dd HH:mm}").FontSize(8);
                row.RelativeItem().AlignCenter().Text(GetStatusText(req.Status)).Bold().FontSize(8.5f).FontColor(GetStatusColor(req.Status));

                string targetWh = req.TargetWarehouse?.NameAr ?? req.TargetWarehouse?.NameEn ?? (_isEnglish ? "Central Warehouse" : "المخزن الرئيسي");
                row.RelativeItem().AlignRight().Text($"{(_isEnglish ? "To: " : "إلى: ")}{targetWh}").FontSize(8);
            });

            // 2. Action Tracking Info Bar
            card.Item().Background(Colors.Orange.Lighten5).Padding(4).Row(row =>
            {
                string reqBy = !string.IsNullOrWhiteSpace(req.Notes) && req.Notes.StartsWith("By:") ? req.Notes.Replace("By:", "").Trim() : "-";
                row.RelativeItem().Text($"{(_isEnglish ? "Requested By: " : "طلب بواسطة: ")}{reqBy}").FontSize(7.5f).Bold();

                string approved = req.ApprovedAt.HasValue
                    ? $"{req.ApprovedByUserId ?? "المخزن"} ({req.ApprovedAt.Value:MM-dd HH:mm})"
                    : "-";
                row.RelativeItem().Text($"{(_isEnglish ? "Approved: " : "الاعتماد: ")}{approved}").FontSize(7.5f);

                string dispatched = req.DispatchedAt.HasValue
                    ? $"{req.DispatchedByUserId ?? "المخزن"} ({req.DispatchedAt.Value:MM-dd HH:mm})"
                    : "-";
                row.RelativeItem().Text($"{(_isEnglish ? "Dispatched: " : "الشحن: ")}{dispatched}").FontSize(7.5f);

                string received = req.ReceivedAt.HasValue
                    ? $"{req.ReceivedByUserId ?? "الفرع"} ({req.ReceivedAt.Value:MM-dd HH:mm})"
                    : "-";
                row.RelativeItem().Text($"{(_isEnglish ? "Received: " : "الاستلام: ")}{received}").FontSize(7.5f).Bold().FontColor(Colors.Green.Darken3);
            });

            // 3. Lines Table
            var lines = req.Lines?.ToList() ?? new List<BranchRequisitionLine>();
            if (lines.Any())
            {
                card.Item().Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(3.5f); // Product Name & Code
                        cd.RelativeColumn(1.2f); // Unit
                        cd.RelativeColumn(1.3f); // Requested Qty
                        cd.RelativeColumn(1.5f); // Proposed Qty
                        cd.RelativeColumn(1.3f); // Approved Qty
                        cd.RelativeColumn(1.3f); // Dispatched Qty
                        cd.RelativeColumn(1.3f); // Received Qty
                        cd.RelativeColumn(1.2f); // Discrepancy
                        cd.RelativeColumn(2.4f); // Notes / Reason
                    });

                    t.Header(h =>
                    {
                        void H(string txt) => h.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text(txt).Bold().FontSize(7.5f);
                        H(_isEnglish ? "Product" : "الصنف والكود");
                        H(_isEnglish ? "Unit" : "الوحدة");
                        H(_isEnglish ? "Requested" : "المطلوب");
                        H(_isEnglish ? "Proposed" : "المقترح/المعدل");
                        H(_isEnglish ? "Approved" : "المعتمد");
                        H(_isEnglish ? "Dispatched" : "المشحون");
                        H(_isEnglish ? "Received" : "المستلم");
                        H(_isEnglish ? "Discrepancy" : "العجز/الفرق");
                        H(_isEnglish ? "Modification Note" : "ملاحظة التعديل / المخزن");
                    });

                    foreach (var line in lines)
                    {
                        string prodName = _isEnglish ? (line.Product?.NameEn ?? line.Product?.NameAr ?? $"Item #{line.ProductId}") : (line.Product?.NameAr ?? line.Product?.NameEn ?? $"صنف #{line.ProductId}");
                        string sku = line.Product?.SKU ?? "";
                        string unitName = _isEnglish ? (line.Unit?.NameEn ?? line.Unit?.Symbol ?? "-") : (line.Unit?.NameAr ?? line.Unit?.Symbol ?? "-");

                        bool isModified = line.ProposedQuantity.HasValue && line.ProposedQuantity.Value != line.RequestedQuantity;

                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(2).AlignRight().Text(txt =>
                        {
                            txt.Span(prodName).FontSize(7.5f).Bold();
                            if (!string.IsNullOrEmpty(sku)) txt.Span($" ({sku})").FontSize(7).FontColor(Colors.Grey.Darken1);
                        });

                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text(unitName).FontSize(7.5f);
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text(line.RequestedQuantity.ToString("G29")).FontSize(7.5f);

                        // Proposed Qty (highlighted if modified)
                        if (isModified)
                            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Background(Colors.Orange.Lighten5).Padding(2).AlignCenter().Text(line.ProposedQuantity!.Value.ToString("G29")).Bold().FontSize(7.5f).FontColor(Colors.Orange.Darken3);
                        else
                            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text("-").FontSize(7.5f).FontColor(Colors.Grey.Darken1);

                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text(line.ApprovedQuantity.ToString("G29")).Bold().FontSize(7.5f);
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text(line.DispatchedQuantity.ToString("G29")).FontSize(7.5f);
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text(line.ReceivedQuantity.ToString("G29")).Bold().FontSize(7.5f).FontColor(Colors.Green.Darken3);

                        // Discrepancy
                        if (line.DiscrepancyQuantity != 0)
                            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Background(Colors.Red.Lighten5).Padding(2).AlignCenter().Text(line.DiscrepancyQuantity.ToString("G29")).Bold().FontSize(7.5f).FontColor(Colors.Red.Darken3);
                        else
                            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text("0").FontSize(7.5f);

                        string modNote = line.ModificationNotes ?? "-";
                        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(2).AlignRight().Text(modNote).FontSize(7).FontColor(isModified ? Colors.Orange.Darken3 : Colors.Grey.Darken2);
                    }
                });
            }
        });
    }

    private void ComposeThermalCard(ColumnDescriptor col, BranchRequisition req)
    {
        col.Item().PaddingTop(3).Border(1).BorderColor(Colors.Black).Padding(2).Column(card =>
        {
            card.Item().Row(r =>
            {
                r.RelativeItem().Text(req.RequisitionNumber).Bold().FontSize(8);
                r.RelativeItem().AlignRight().Text(GetStatusText(req.Status)).Bold().FontSize(7.5f);
            });
            card.Item().Text($"{(_isEnglish ? "Date: " : "التاريخ: ")}{req.RequestDate:MM/dd HH:mm}").FontSize(7);

            var lines = req.Lines?.ToList() ?? new List<BranchRequisitionLine>();
            if (lines.Any())
            {
                card.Item().PaddingTop(2).Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(5);
                        cd.RelativeColumn(2.5f);
                        cd.RelativeColumn(2.5f);
                    });

                    t.Header(h =>
                    {
                        h.Cell().AlignRight().Text(_isEnglish ? "Item" : "الصنف").Bold().FontSize(7);
                        h.Cell().AlignCenter().Text(_isEnglish ? "Req" : "مطلوب").Bold().FontSize(7);
                        h.Cell().AlignCenter().Text(_isEnglish ? "Rec" : "مستلم").Bold().FontSize(7);
                    });

                    foreach (var l in lines)
                    {
                        string name = l.Product?.NameAr ?? l.Product?.NameEn ?? $"#{l.ProductId}";
                        t.Cell().AlignRight().Text(name).FontSize(7);
                        t.Cell().AlignCenter().Text(l.RequestedQuantity.ToString("G29")).FontSize(7);
                        t.Cell().AlignCenter().Text(l.ReceivedQuantity.ToString("G29")).Bold().FontSize(7);
                    }
                });
            }
        });
    }

    private void ComposeFooter(ColumnDescriptor col)
    {
        col.Item().PaddingTop(8).AlignCenter().Text(text =>
        {
            text.Span(_isEnglish ? "Printed at: " : "طبع في: ").FontSize(7);
            text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).FontSize(7);
        });
    }
}
