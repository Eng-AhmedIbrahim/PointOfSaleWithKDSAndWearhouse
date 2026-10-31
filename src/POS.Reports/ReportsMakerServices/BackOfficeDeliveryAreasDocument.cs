namespace POS.Reports.ReportsMakerServices;

public class DeliveryAreaReportItem
{
    public string ZoneName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageOrderValue => TotalOrders > 0 ? TotalAmount / TotalOrders : 0;
}

public class BackOfficeDeliveryAreasDocument : IDocument
{
    private readonly List<DeliveryAreaReportItem> _areas;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;
    private const string BaseFont = "Cairo";
    private readonly string _headerBg = "#e65100";

    public BackOfficeDeliveryAreasDocument(
        List<DeliveryAreaReportItem> areas,
        string branchName,
        DateTime fromDate,
        DateTime toDate,
        bool isThermal,
        string? language)
    {
        _areas = areas;
        _branchName = branchName;
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
            c.Item().AlignCenter().Text(_branchName).FontSize(_isThermal ? 11 : 18).Bold().FontColor(_headerBg);
            c.Item().PaddingTop(2).AlignCenter().Text(_isEnglish ? "Top Delivery Areas Report" : "تقرير مناطق التوصيل الأعلى").FontSize(_isThermal ? 9 : 14).Bold();
            c.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(_headerBg);
            c.Item().PaddingTop(4).AlignCenter()
                .Text($"{(_isEnglish ? "Period" : "الفترة")}: {_fromDate:yyyy-MM-dd}  {(_isEnglish ? "to" : "إلى")}  {_toDate:yyyy-MM-dd}")
                .FontSize(_isThermal ? 7 : 9).FontColor(Colors.Grey.Darken1);
            c.Item().PaddingTop(2).AlignCenter()
                .Text($"{(_isEnglish ? "Total Zones" : "إجمالي المناطق")}: {_areas.Count}")
                .FontSize(_isThermal ? 7 : 9).Bold();
            c.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Table(t =>
        {
            if (_isThermal)
            {
                // RTL thermal: المبلغ | الطلبات | المنطقة
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3); // المنطقة
                    c.RelativeColumn(1); // الطلبات
                    c.RelativeColumn(2); // المبلغ
                });

                t.Header(h =>
                {
                    h.Cell().Padding(2).Background(_headerBg).Text(_isEnglish ? "Zone" : "المنطقة").Bold().FontColor(Colors.White).FontSize(8);
                    h.Cell().Padding(2).Background(_headerBg).AlignCenter().Text(_isEnglish ? "Ords" : "الطلبات").Bold().FontColor(Colors.White).FontSize(8);
                    h.Cell().Padding(2).Background(_headerBg).AlignRight().Text(_isEnglish ? "Amt" : "المبلغ").Bold().FontColor(Colors.White).FontSize(8);
                });

                for (int i = 0; i < _areas.Count; i++)
                {
                    var item = _areas[i];
                    var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                    t.Cell().Background(bg).Padding(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Text(item.ZoneName ?? "-").FontSize(7);
                    t.Cell().Background(bg).Padding(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).AlignCenter().Text(item.TotalOrders.ToString()).FontSize(7);
                    t.Cell().Background(bg).Padding(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).AlignRight().Text(item.TotalAmount.ToString("0.##")).Bold().FontSize(7);
                }
            }
            else
            {
                // RTL A4: الإيرادات | متوسط الطلب | الطلبات | المنطقة
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2); // الإيرادات
                    c.RelativeColumn(2); // متوسط
                    c.RelativeColumn(1); // الطلبات
                    c.RelativeColumn(3); // المنطقة
                });

                t.Header(h =>
                {
                    void HCell(string txt) =>
                        h.Cell().Background(_headerBg).Padding(6).Border(0.5f).BorderColor(_headerBg)
                            .Text(txt).Bold().FontColor(Colors.White).AlignCenter().FontSize(10);

                    HCell(_isEnglish ? "Total Revenue" : "إجمالي الإيرادات");
                    HCell(_isEnglish ? "Avg / Order" : "متوسط الطلب");
                    HCell(_isEnglish ? "Orders" : "الطلبات");
                    HCell(_isEnglish ? "Delivery Zone" : "منطقة التوصيل");
                });

                for (int i = 0; i < _areas.Count; i++)
                {
                    var item = _areas[i];
                    var bg = i % 2 == 0 ? (string)Colors.White : "#fff8f0";

                    t.Cell().Background(bg).Padding(5).Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .AlignCenter().Text(item.TotalAmount.ToString("N2")).Bold().FontColor(_headerBg).FontSize(10);
                    t.Cell().Background(bg).Padding(5).Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .AlignCenter().Text(item.AverageOrderValue.ToString("N2")).FontSize(10);
                    t.Cell().Background(bg).Padding(5).Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .AlignCenter().Text(item.TotalOrders.ToString()).Bold().FontSize(10);
                    t.Cell().Background(bg).Padding(5).Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .AlignCenter().Text(item.ZoneName ?? "-").Bold().FontSize(10);
                }

                // Totals
                t.Cell().Background(Colors.Grey.Lighten3).Padding(6).Border(0.5f).BorderColor(Colors.Grey.Medium)
                    .Text(_areas.Sum(a => a.TotalAmount).ToString("N2")).Bold().AlignCenter().FontColor(_headerBg).FontSize(10);
                t.Cell().Background(Colors.Grey.Lighten3).Padding(6).Border(0.5f).BorderColor(Colors.Grey.Medium)
                    .Text((_areas.Count > 0 ? _areas.Sum(a => a.TotalAmount) / _areas.Sum(a => a.TotalOrders == 0 ? 1 : a.TotalOrders) : 0).ToString("N2")).Bold().AlignCenter().FontSize(10);
                t.Cell().Background(Colors.Grey.Lighten3).Padding(6).Border(0.5f).BorderColor(Colors.Grey.Medium)
                    .Text(_areas.Sum(a => a.TotalOrders).ToString()).Bold().AlignCenter().FontSize(10);
                t.Cell().Background(Colors.Grey.Lighten3).Padding(6).Border(0.5f).BorderColor(Colors.Grey.Medium)
                    .Text(_isEnglish ? "TOTAL" : "الإجمالي").Bold().AlignCenter().FontSize(10);
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
