using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POS.Reports.ReportsMakerServices;

public class CustomerReportItem
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? LastOrderDate { get; set; }
}

public class BackOfficeCustomerReportDocument : IDocument
{
    private readonly List<CustomerReportItem> _customers;
    private readonly string _branchName;
    private readonly string _reportTitle;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;
    private const string BaseFont = "Cairo";
    private readonly string _headerBg = "#1a237e";
    private readonly string _rowAltBg = "#f5f7ff";

    public BackOfficeCustomerReportDocument(
        List<CustomerReportItem> customers,
        string branchName,
        string reportTitle,
        DateTime fromDate,
        DateTime toDate,
        bool isThermal,
        string? language)
    {
        _customers = customers;
        _branchName = branchName;
        _reportTitle = reportTitle;
        _fromDate = fromDate;
        _toDate = toDate;
        _isThermal = isThermal;
        _isEnglish = (language ?? "ar").ToLower() == "en";
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            if (_isThermal)
            {
                page.ContinuousSize(8f, Unit.Centimetre);
                page.Margin(3, Unit.Millimetre);
            }
            else
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
            }

            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontFamily(BaseFont).FontSize(_isThermal ? 8 : 10));

            if (!_isEnglish)
                page.ContentFromRightToLeft();

            page.Content().Column(col =>
            {
                col.Spacing(_isThermal ? 3 : 6);
                col.Item().Element(ComposeHeader);
                col.Item().Element(ComposeContent);
            });

            if (!_isThermal)
                page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(c =>
        {
            c.Item().AlignCenter().Text(_branchName)
                .FontSize(_isThermal ? 11 : 18).Bold().FontColor(_headerBg);

            c.Item().PaddingTop(2).AlignCenter().Text(_reportTitle)
                .FontSize(_isThermal ? 9 : 14).Bold();

            c.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(_headerBg);

            c.Item().PaddingTop(4).AlignCenter()
                .Text($"{(_isEnglish ? "Period" : "الفترة")}: {_fromDate:yyyy-MM-dd}  {(_isEnglish ? "to" : "إلى")}  {_toDate:yyyy-MM-dd}")
                .FontSize(_isThermal ? 7 : 9).FontColor(Colors.Grey.Darken1);

            c.Item().PaddingTop(2).AlignCenter()
                .Text($"{(_isEnglish ? "Total Customers" : "إجمالي العملاء")}: {_customers.Count}")
                .FontSize(_isThermal ? 7 : 9).Bold();

            c.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Table(t =>
        {
            // === THERMAL: 3 columns reversed for RTL ===
            if (_isThermal)
            {
                t.ColumnsDefinition(c =>
                {
                    // RTL order: العميل | طلبات/إجمالي | آخر طلب
                    c.RelativeColumn(3); // العميل
                    c.RelativeColumn(2); // طلبات/إجمالي
                    c.RelativeColumn(2); // آخر طلب
                });

                t.Header(h =>
                {
                    h.Cell().Padding(2).Background(_headerBg).Text(_isEnglish ? "Customer" : "العميل").Bold().FontColor(Colors.White).FontSize(8);
                    h.Cell().Padding(2).Background(_headerBg).AlignCenter().Text(_isEnglish ? "Ords/Amt" : "طلبات/إجمالي").Bold().FontColor(Colors.White).FontSize(8);
                    h.Cell().Padding(2).Background(_headerBg).AlignRight().Text(_isEnglish ? "Last Order" : "آخر طلب").Bold().FontColor(Colors.White).FontSize(8);
                });

                for (int i = 0; i < _customers.Count; i++)
                {
                    var item = _customers[i];
                    var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                    t.Cell().Background(bg).Padding(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Text($"{item.Name}\n{item.Phone}").FontSize(7);
                    t.Cell().Background(bg).Padding(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).AlignCenter()
                        .Text($"{item.TotalOrders}\n{item.TotalAmount:0.##}").FontSize(7);
                    t.Cell().Background(bg).Padding(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).AlignRight()
                        .Text(item.LastOrderDate?.ToString("yyyy-MM-dd") ?? "-").FontSize(7);
                }
            }
            // === A4: 6 columns reversed for RTL ===
            else
            {
                t.ColumnsDefinition(c =>
                {
                    // RTL order: آخر طلب | المبلغ | الطلبات | العنوان | الموبايل | العميل
                    c.RelativeColumn(2); // آخر طلب
                    c.RelativeColumn(2); // المبلغ
                    c.RelativeColumn(1); // الطلبات
                    c.RelativeColumn(3); // العنوان
                    c.RelativeColumn(2); // الموبايل
                    c.RelativeColumn(2); // العميل
                });

                t.Header(h =>
                {
                    void HeaderCell(string text) =>
                        h.Cell().Background(_headerBg).Padding(6).Border(0.5f).BorderColor(_headerBg)
                            .Text(text).Bold().FontColor(Colors.White).AlignCenter().FontSize(10);

                    HeaderCell(_isEnglish ? "Last Order" : "آخر طلب");
                    HeaderCell(_isEnglish ? "Amount" : "المبلغ");
                    HeaderCell(_isEnglish ? "Orders" : "الطلبات");
                    HeaderCell(_isEnglish ? "Address" : "العنوان");
                    HeaderCell(_isEnglish ? "Phone" : "الموبايل");
                    HeaderCell(_isEnglish ? "Customer" : "العميل");
                });

                for (int i = 0; i < _customers.Count; i++)
                {
                    var item = _customers[i];
                    var bg = i % 2 == 0 ? (string)Colors.White : _rowAltBg;

                    void DataCell(string text, bool isBold = false)
                    {
                        var cell = t.Cell().Background(bg).Padding(5).Border(0.5f).BorderColor(Colors.Grey.Lighten2).AlignCenter();
                        if (isBold) cell.Text(text).Bold().FontSize(10);
                        else cell.Text(text).FontSize(10);
                    }

                    DataCell(item.LastOrderDate?.ToString("yyyy-MM-dd") ?? "-");
                    DataCell(item.TotalAmount.ToString("N2"), isBold: true);
                    DataCell(item.TotalOrders.ToString(), isBold: true);
                    DataCell(item.Address ?? "-");
                    DataCell(item.Phone ?? "-");
                    DataCell(item.Name ?? "-", isBold: true);
                }

                // Totals row
                t.Cell().ColumnSpan(5).Background(Colors.Grey.Lighten3).Padding(6).Border(0.5f).BorderColor(Colors.Grey.Medium)
                    .Text(_isEnglish ? "TOTAL" : "الإجمالي").Bold().AlignCenter();
                t.Cell().Background(Colors.Grey.Lighten3).Padding(6).Border(0.5f).BorderColor(Colors.Grey.Medium)
                    .Text(_customers.Sum(c => c.TotalAmount).ToString("N2")).Bold().AlignCenter().FontColor(_headerBg);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(c =>
        {
            c.Item().PaddingTop(4).LineHorizontal(1f).LineColor(_headerBg);
            c.Item().PaddingTop(4).Row(r =>
            {
                r.RelativeItem().Text($"{(_isEnglish ? "Printed" : "طُبع")}: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                r.RelativeItem().AlignCenter().Text(x =>
                {
                    x.Span(_isEnglish ? "Page " : "صفحة ").FontSize(8);
                    x.CurrentPageNumber().FontSize(8).Bold();
                    x.Span(_isEnglish ? " of " : " من ").FontSize(8);
                    x.TotalPages().FontSize(8).Bold();
                });
                r.RelativeItem().AlignRight().Text(_branchName).FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        });
    }
}
