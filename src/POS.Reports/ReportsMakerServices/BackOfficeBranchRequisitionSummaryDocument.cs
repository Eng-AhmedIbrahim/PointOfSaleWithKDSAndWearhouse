using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using POS.Core.Entities.Warehouse;
using System.Collections.Generic;
using System.Linq;
using System;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficeBranchRequisitionSummaryDocument : IDocument
{
    private readonly List<BranchRequisition> _requisitions;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly string _reportTitle;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficeBranchRequisitionSummaryDocument(
        List<BranchRequisition> requisitions,
        string branchName,
        DateTime fromDate,
        DateTime toDate,
        string reportTitle = "تقرير طلبيات المخزن الرئيسي",
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
                col.Spacing(_isThermal ? 3 : 5);
                ComposeHeader(col);
                ComposeKpiSummary(col);
                ComposeRequisitionsTable(col);
                ComposeFooter(col);
            });
        });
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        col.Item().AlignCenter().Text(_branchName).FontSize(_isThermal ? 11 : 14).Bold().FontColor(Colors.Black);
        col.Item().AlignCenter().Text(_reportTitle).FontSize(_isThermal ? 12 : 16).Bold().FontColor(Colors.Orange.Darken3);
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
            RequisitionStatus.ModificationProposed => "تم اقتراح تعديل",
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

    private void ComposeKpiSummary(ColumnDescriptor col)
    {
        int total = _requisitions.Count;
        int received = _requisitions.Count(r => r.Status == RequisitionStatus.Received);
        int inTransit = _requisitions.Count(r => r.Status == RequisitionStatus.InTransit || r.Status == RequisitionStatus.Preparing);
        int approved = _requisitions.Count(r => r.Status == RequisitionStatus.Approved || r.Status == RequisitionStatus.UnderReview);
        int rejected = _requisitions.Count(r => r.Status == RequisitionStatus.Rejected || r.Status == RequisitionStatus.Cancelled);

        if (_isThermal)
        {
            col.Item().Background(Colors.Grey.Lighten4).Padding(3).Column(c =>
            {
                c.Item().Row(r =>
                {
                    r.RelativeItem().Text(_isEnglish ? "Total Requisitions:" : "إجمالي الطلبيات:").Bold().FontSize(8);
                    r.RelativeItem().AlignRight().Text(total.ToString()).Bold().FontSize(8);
                });
                c.Item().Row(r =>
                {
                    r.RelativeItem().Text(_isEnglish ? "Received:" : "المستلمة:").Bold().FontSize(8);
                    r.RelativeItem().AlignRight().Text(received.ToString()).Bold().FontSize(8).FontColor(Colors.Green.Darken2);
                });
                c.Item().Row(r =>
                {
                    r.RelativeItem().Text(_isEnglish ? "In Transit:" : "بالطريق / جاري التجهيز:").Bold().FontSize(8);
                    r.RelativeItem().AlignRight().Text(inTransit.ToString()).Bold().FontSize(8).FontColor(Colors.Blue.Darken2);
                });
            });
        }
        else
        {
            col.Item().PaddingVertical(3).Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Row(row =>
            {
                void Kpi(string label, int val, string color)
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().AlignCenter().Text(label).FontSize(8).FontColor(Colors.Grey.Darken2);
                        c.Item().AlignCenter().Text(val.ToString()).FontSize(12).Bold().FontColor(color);
                    });
                }

                Kpi(_isEnglish ? "Total Requests" : "إجمالي الطلبيات", total, Colors.Indigo.Darken3);
                Kpi(_isEnglish ? "Received" : "تم الاستلام", received, Colors.Green.Darken3);
                Kpi(_isEnglish ? "In Transit" : "تم الشحن / بالطريق", inTransit, Colors.Blue.Darken3);
                Kpi(_isEnglish ? "Under Review" : "قيد المراجعة / معتمد", approved, Colors.Orange.Darken3);
                Kpi(_isEnglish ? "Rejected / Cancelled" : "مرفوضة / ملغاة", rejected, Colors.Red.Darken3);
            });
        }
    }

    private void ComposeRequisitionsTable(ColumnDescriptor col)
    {
        if (!_requisitions.Any())
        {
            col.Item().Padding(10).AlignCenter().Text(_isEnglish ? "No requisitions found for this period." : "لا توجد طلبيات مسجلة لهذه الفترة.").FontSize(9).FontColor(Colors.Grey.Darken1);
            return;
        }

        col.Item().PaddingTop(4).Table(t =>
        {
            if (_isThermal)
            {
                t.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(3); // Req #
                    cd.RelativeColumn(3); // Time
                    cd.RelativeColumn(2.5f); // Status
                    cd.RelativeColumn(1.5f); // Qty
                });

                t.Header(h =>
                {
                    h.Cell().BorderBottom(1).Padding(1).AlignRight().Text(_isEnglish ? "Req#" : "الطلب").Bold().FontSize(7);
                    h.Cell().BorderBottom(1).Padding(1).AlignCenter().Text(_isEnglish ? "Time" : "الوقت").Bold().FontSize(7);
                    h.Cell().BorderBottom(1).Padding(1).AlignCenter().Text(_isEnglish ? "Status" : "الحالة").Bold().FontSize(7);
                    h.Cell().BorderBottom(1).Padding(1).AlignCenter().Text(_isEnglish ? "Qty" : "الكمية").Bold().FontSize(7);
                });

                foreach (var req in _requisitions.OrderByDescending(r => r.RequestDate))
                {
                    decimal totQty = req.Lines?.Sum(l => l.RequestedQuantity) ?? 0;
                    t.Cell().BorderBottom(0.5f).Padding(1).AlignRight().Text(req.RequisitionNumber).FontSize(7);
                    t.Cell().BorderBottom(0.5f).Padding(1).AlignCenter().Text(req.RequestDate.ToString("MM/dd HH:mm")).FontSize(7);
                    t.Cell().BorderBottom(0.5f).Padding(1).AlignCenter().Text(GetStatusText(req.Status)).Bold().FontSize(7).FontColor(GetStatusColor(req.Status));
                    t.Cell().BorderBottom(0.5f).Padding(1).AlignCenter().Text(totQty.ToString("G29")).FontSize(7);
                }
            }
            else
            {
                t.ColumnsDefinition(cd =>
                {
                    cd.ConstantColumn(90);  // Req #
                    cd.ConstantColumn(100); // Request Date/Time
                    cd.ConstantColumn(85);  // Requested By
                    cd.ConstantColumn(95);  // Status
                    cd.ConstantColumn(110); // Approved By & Date
                    cd.ConstantColumn(110); // Received By & Date
                    cd.ConstantColumn(50);  // Items
                    cd.ConstantColumn(65);  // Total Qty
                    cd.RelativeColumn();    // Notes
                });

                t.Header(h =>
                {
                    void H(string txt) => h.Cell().Background(Colors.Orange.Darken3).Padding(3).AlignCenter().Text(txt).Bold().FontSize(8).FontColor(Colors.White);
                    H(_isEnglish ? "Req #" : "رقم الطلبية");
                    H(_isEnglish ? "Request Date" : "تاريخ ووقت الطلب");
                    H(_isEnglish ? "Requested By" : "مسؤول الطلب");
                    H(_isEnglish ? "Status" : "حالة الطلبية");
                    H(_isEnglish ? "Approved By / At" : "الاعتماد (المسؤول / الوقت)");
                    H(_isEnglish ? "Received By / At" : "الاستلام (المسؤول / الوقت)");
                    H(_isEnglish ? "Items" : "الأصناف");
                    H(_isEnglish ? "Total Qty" : "إجمالي الكمية");
                    H(_isEnglish ? "Notes / Reason" : "ملاحظات / سبب");
                });

                int idx = 0;
                foreach (var req in _requisitions.OrderByDescending(r => r.RequestDate))
                {
                    var bg = idx % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                    idx++;

                    void C(string txt, bool bold = false, string? clr = null)
                    {
                        var cell = t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(3).AlignCenter().Text(txt ?? "-").FontSize(7.5f);
                        if (bold) cell.Bold();
                        if (!string.IsNullOrEmpty(clr)) cell.FontColor(clr);
                    }

                    int itemsCount = req.Lines?.Count ?? 0;
                    decimal totalQty = req.Lines?.Sum(l => l.RequestedQuantity) ?? 0;

                    C(req.RequisitionNumber, bold: true, clr: Colors.Blue.Darken3);
                    C(req.RequestDate.ToString("yyyy-MM-dd HH:mm"));
                    C(!string.IsNullOrWhiteSpace(req.Notes) && req.Notes.StartsWith("By:") ? req.Notes.Replace("By:", "").Trim() : "-");
                    C(GetStatusText(req.Status), bold: true, clr: GetStatusColor(req.Status));

                    string approvedStr = req.ApprovedAt.HasValue
                        ? $"{req.ApprovedByUserId ?? "المخزن"} ({req.ApprovedAt.Value:MM-dd HH:mm})"
                        : "-";
                    C(approvedStr);

                    string receivedStr = req.ReceivedAt.HasValue
                        ? $"{req.ReceivedByUserId ?? "الفرع"} ({req.ReceivedAt.Value:MM-dd HH:mm})"
                        : "-";
                    C(receivedStr);

                    C(itemsCount.ToString());
                    C(totalQty.ToString("G29"), bold: true);
                    C(req.RejectionReason ?? req.Notes ?? "-");
                }
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
