using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficeStockMovementDocument : IDocument
{
    private readonly IEnumerable<dynamic> _transactions;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficeStockMovementDocument(IEnumerable<dynamic> transactions, string branchName, DateTime fromDate, DateTime toDate, string lang = "ar", bool isThermal = false)
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
        col.Item().AlignCenter().Text(_isEnglish ? "Stock Movements Report" : "حركات الأصناف التفصيلية").FontSize(_isThermal ? 12 : 18).Bold().FontColor(Colors.Black);
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
                cd.RelativeColumn(2); // Type
                cd.RelativeColumn(1.5f); // Change
                cd.RelativeColumn(1.5f); // Result
            });

            t.Header(h =>
            {
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Item" : "الصنف").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Time" : "الوقت").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Type" : "النوع").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "±" : "الحركة").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Result" : "الرصيد").Bold();
            });

            foreach (var trans in _transactions)
            {
                t.Cell().Border(1).Padding(1).AlignRight().Text((string)trans.ItemName).FontSize(_isThermal ? 7 : 9);
                t.Cell().Border(1).Padding(1).AlignCenter().Text((string)trans.TimeDisplay).FontSize(_isThermal ? 7 : 9);
                t.Cell().Border(1).Padding(1).AlignCenter().Text((string)trans.Type).FontSize(_isThermal ? 7 : 9);
                
                var change = (decimal)trans.QtyChange;
                var color = change > 0 ? Colors.Green.Medium : (change < 0 ? Colors.Red.Medium : Colors.Black);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(change.ToString("0.##")).FontColor(color).Bold();
                
                t.Cell().Border(1).Padding(1).AlignCenter().Text(((decimal)trans.ResultQty).ToString("0.##")).Bold();
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
