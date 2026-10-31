namespace POS.Reports.ReportsMakerServices;

public class BackOfficeHourlySalesDocument : IDocument
{
    private readonly List<HourlySalesDto> _data;
    private readonly string _branchName;
    private readonly DateTime _reportDate;
    private readonly string _currency;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficeHourlySalesDocument(List<HourlySalesDto> data, string branchName, DateTime reportDate, bool isThermal = false, string lang = "ar", string currency = "EGP")
    {
        _data = data ?? new List<HourlySalesDto>();
        _branchName = branchName;
        _reportDate = reportDate;
        _isThermal = isThermal;
        _currency = currency ?? "EGP";
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
                ComposeHighlights(col);
                ComposeHourlyTable(col);
                ComposeFooter(col);
            });
        });
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        // Branch Name (Black, Center)
        col.Item().AlignCenter().Text(_branchName).FontSize(_isThermal ? 11 : 14).Bold().FontColor(Colors.Black);
        
        // Report Title (Black, Center)
        col.Item().AlignCenter().Text(_isEnglish ? "Hourly Sales Analysis" : "تحليل المبيعات بالساعة").FontSize(_isThermal ? 12 : 18).Bold().FontColor(Colors.Black);
        
        // Subtitle
        col.Item().AlignCenter().Text(_isEnglish ? $"Date: {_reportDate:dd/MM/yyyy}" : $"التاريخ: {_reportDate:dd/MM/yyyy}").FontSize(_isThermal ? 7 : 10).FontColor(Colors.Grey.Darken2);
        
        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
    }

    private void ComposeHighlights(ColumnDescriptor col)
    {
        var maxAmtItem = _data.OrderByDescending(x => x.Amount).FirstOrDefault();
        var maxCntItem = _data.OrderByDescending(x => x.OrderCount).FirstOrDefault();

        col.Item().PaddingVertical(_isThermal ? 2 : 8).Row(row =>
        {
            row.RelativeItem().Padding(2).Background(Colors.Amber.Lighten5).Column(c =>
            {
                c.Item().Text(_isEnglish ? "Peak Sale" : "ذروة المبيعات").FontSize(_isThermal ? 7 : 9).Bold();
                c.Item().Text($"{maxAmtItem?.HourLabel} - {maxAmtItem?.Amount:0.##}").FontSize(_isThermal ? 9 : 11).Bold().FontColor(Colors.Amber.Medium);
            });
            row.RelativeItem().Padding(2).Background(Colors.Blue.Lighten5).Column(c =>
            {
                c.Item().Text(_isEnglish ? "Busiest Hour" : "أكثر ساعة طلباً").FontSize(_isThermal ? 7 : 9).Bold();
                c.Item().Text($"{maxCntItem?.HourLabel} - {maxCntItem?.OrderCount}").FontSize(_isThermal ? 9 : 11).Bold().FontColor(Colors.Blue.Medium);
            });
        });
    }

    private void ComposeHourlyTable(ColumnDescriptor col)
    {
        col.Item().PaddingTop(5).Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(2.5f); // Hour
                cd.RelativeColumn(2); // Amount
                cd.RelativeColumn(1.5f); // Orders
                cd.RelativeColumn(2); // Avg
                cd.RelativeColumn(1); // Trend
            });

            t.Header(h =>
            {
                h.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text(_isEnglish ? "Hour" : "الساعة").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text(_isEnglish ? "Amount" : "المبلغ").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text(_isEnglish ? "Qty" : "عدد").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text(_isEnglish ? "Avg" : "متوسط").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text("").Bold();
            });

            var totalAmount = _data.Sum(x => x.Amount);
            
            foreach (var h in _data)
            {
                t.Cell().Border(1).Padding(2).AlignCenter().Text(h.HourLabel);
                t.Cell().Border(1).Padding(2).AlignCenter().Text(h.Amount.ToString("0.##"));
                t.Cell().Border(1).Padding(2).AlignCenter().Text(h.OrderCount.ToString());
                t.Cell().Border(1).Padding(2).AlignCenter().Text((h.OrderCount > 0 ? h.Amount / h.OrderCount : 0).ToString("0.##"));
                
                decimal share = totalAmount > 0 ? (h.Amount / totalAmount * 100) : 0;
                string trend = share > 10 ? "🔥🔥" : (share > 5 ? "🔥" : "");
                t.Cell().Border(1).Padding(2).AlignCenter().Text(trend);
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
