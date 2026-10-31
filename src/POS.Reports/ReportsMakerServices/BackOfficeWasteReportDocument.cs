using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficeWasteReportDocument : IDocument
{
    private readonly IEnumerable<dynamic> _transactions;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficeWasteReportDocument(IEnumerable<dynamic> transactions, string branchName, DateTime fromDate, DateTime toDate, string lang = "ar", bool isThermal = false)
    {
        _transactions = transactions ?? new List<dynamic>();
        _branchName = branchName;
        _fromDate = fromDate;
        _toDate = toDate;
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
                ComposeTransactionsTable(col);
                ComposeFooter(col);
            });
        });
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        col.Item().AlignCenter().Text(_branchName).FontSize(_isThermal ? 11 : 14).Bold().FontColor(Colors.Black);
        col.Item().AlignCenter().Text(_isEnglish ? "Waste & Damage Report" : "تقرير الهوالك والتالف").FontSize(_isThermal ? 12 : 18).Bold().FontColor(Colors.Black);
        col.Item().AlignCenter().Text(_isEnglish ? $"Period: {_fromDate:yyyy/MM/dd} - {_toDate:yyyy/MM/dd}" : $"الفترة من: {_fromDate:yyyy/MM/dd} إلى: {_toDate:yyyy/MM/dd}").FontSize(_isThermal ? 7 : 10).FontColor(Colors.Grey.Darken2);
        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
    }

    private void ComposeTransactionsTable(ColumnDescriptor col)
    {
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(3); // Item
                cd.RelativeColumn(2); // Date/Time
                cd.RelativeColumn(1.5f); // Qty
                cd.RelativeColumn(3.5f); // Reason/Note
            });

            t.Header(h =>
            {
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Item" : "الصنف").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Time" : "الوقت").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Qty" : "الكمية").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Reason" : "السبب/الملاحظة").Bold();
            });

            foreach (var trans in _transactions)
            {
                t.Cell().Border(1).Padding(1).AlignRight().Text((string)trans.ItemName).FontSize(_isThermal ? 7 : 9);
                t.Cell().Border(1).Padding(1).AlignCenter().Text((string)trans.TimeDisplay).FontSize(_isThermal ? 7 : 9);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(((decimal)trans.QtyChange).ToString("0.##")).Bold();
                
                string reason = (string)trans.Reason;
                string notes = (string)trans.Notes;
                string fullNote = !string.IsNullOrEmpty(reason) ? reason : (!string.IsNullOrEmpty(notes) ? notes : "-");
                t.Cell().Border(1).Padding(1).AlignRight().Text(fullNote).FontSize(_isThermal ? 7 : 8);
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
