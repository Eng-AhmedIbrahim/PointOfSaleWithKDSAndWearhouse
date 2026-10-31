namespace POS.Reports.ReportsMakerServices;

public class BackOfficeItemSalesDocument : IDocument
{
    private readonly List<SalesItemSummaryDto> _items;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly string _reportTitle;
    private readonly bool _isThermal;
    private readonly bool _isEnglish;

    public BackOfficeItemSalesDocument(List<SalesItemSummaryDto> items, string branchName, DateTime fromDate, DateTime toDate, string reportTitle = "تقرير مبيعات الأصناف", string lang = "ar", bool isThermal = false)
    {
        _items = items ?? new List<SalesItemSummaryDto>();
        _branchName = branchName;
        _fromDate = fromDate;
        _toDate = toDate;
        _reportTitle = reportTitle;
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
                page.Margin(0.5f, Unit.Centimetre);
            }
            page.PageColor(Colors.White);
            
            var baseStyle = TextStyle.Default.FontFamily("Arial", "Segoe UI", "Tahoma", "Noto Sans Arabic").FontSize(_isThermal ? 9 : 11);
            page.DefaultTextStyle(baseStyle);

            if (!_isEnglish)
                page.ContentFromRightToLeft();

            page.Content().Column(col =>
            {
                ComposeHeader(col);
                ComposeItemsTable(col);
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
        
        // Subtitle
        col.Item().AlignCenter().Text(_isEnglish ? $"Period: {_fromDate:dd/MM/yyyy} to {_toDate:dd/MM/yyyy}" : $"الفترة من: {_fromDate:dd/MM/yyyy} إلى: {_toDate:dd/MM/yyyy}").FontSize(_isThermal ? 7 : 10).FontColor(Colors.Grey.Darken3);
        
        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
    }

    static IContainer HeaderCell(IContainer container) => container.Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter();
    static IContainer DataCell(IContainer container) => container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4);

    private void ComposeItemsTable(ColumnDescriptor col)
    {
        col.Item().PaddingTop(10).Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(4); // Name
                cd.RelativeColumn(2); // Category
                cd.RelativeColumn(1.5f); // Qty
                cd.RelativeColumn(2); // Total
            });

            t.Header(h =>
            {
                h.Cell().Element(HeaderCell).AlignRight().Text(_isEnglish ? "Item" : "الصنف").Bold();
                h.Cell().Element(HeaderCell).Text(_isEnglish ? "Category" : "الفئة").Bold();
                h.Cell().Element(HeaderCell).Text(_isEnglish ? "Qty" : "الكمية").Bold();
                h.Cell().Element(HeaderCell).AlignRight().Text(_isEnglish ? "Total" : "الإجمالي").Bold();
            });

            for (int i = 0; i < _items.Count; i++)
            {
                var itm = _items[i];
                var bgColor = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;

                t.Cell().Background(bgColor).Element(DataCell).AlignRight().Text(itm.ItemName).Bold();
                t.Cell().Background(bgColor).Element(DataCell).AlignCenter().Text(itm.CategoryName);
                t.Cell().Background(bgColor).Element(DataCell).AlignCenter().Text(itm.Quantity.ToString("0.##"));
                t.Cell().Background(bgColor).Element(DataCell).AlignRight().Text(itm.TotalAmount.ToString("0.##") ).Bold();
            }

            // Total Row
            t.Cell().ColumnSpan(3).Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(5).AlignRight().Text(_isEnglish ? "Grand Total" : "الإجمالي الكلي").Bold();
            t.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(5).AlignRight().Text(_items.Sum(x => x.TotalAmount).ToString("0.##") ).Bold();
        });
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
