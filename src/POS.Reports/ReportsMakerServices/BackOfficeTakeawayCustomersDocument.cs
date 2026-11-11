using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POS.Reports.ReportsMakerServices;

public class TakeawayCustomerReportItem
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public bool IsRegistered { get; set; }
    public int TotalOrders { get; set; }
    public int VoidedOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime? FirstOrderDate { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public List<TakeawayOrderRow> Orders { get; set; } = new();
}

public class TakeawayOrderItemRow
{
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public List<string> Attributes { get; set; } = new();
}

public class TakeawayOrderRow
{
    public int OrderId { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal GrandTotal { get; set; }
    public string State { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public List<TakeawayOrderItemRow> Items { get; set; } = new();
}

public class BackOfficeTakeawayCustomersDocument : IDocument
{
    private readonly List<TakeawayCustomerReportItem> _customers;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly bool _isThermal;
    private readonly bool _isEnglish;
    private const string Currency = "ج.م";

    public BackOfficeTakeawayCustomersDocument(
        List<TakeawayCustomerReportItem> customers,
        string branchName,
        DateTime fromDate,
        DateTime toDate,
        bool isThermal,
        string? language)
    {
        _customers = customers ?? new List<TakeawayCustomerReportItem>();
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
                page.Margin(2, Unit.Millimetre);
            }
            else
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.Header().Element(ComposeHeader);
                page.Footer().Element(ComposeFooter);
            }

            page.PageColor(Colors.White);
            var baseStyle = TextStyle.Default
                .FontFamily("Arial", "Noto Sans Arabic")
                .FontSize(_isThermal ? 8 : 9);
            page.DefaultTextStyle(baseStyle);

            if (!_isEnglish)
                page.ContentFromRightToLeft();

            page.Content().Column(col =>
            {
                col.Spacing(_isThermal ? 2 : 6);
                if (_isThermal)
                {
                    ComposeThermal(col);
                }
                else
                {
                    ComposeKpiCards(col);
                    ComposeBarChart(col);
                    ComposeSummaryTable(col);
                    ComposeCustomerDetails(col);
                }
            });
        });
    }

    // ─── Header / Footer ──────────────────────────────────────────────────────

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Background("#0f172a").Padding(12).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(_branchName).FontSize(14).Bold().FontColor(Colors.White);
                    c.Item().Text(_isEnglish ? "TakeAway Customers Report" : "تقرير عملاء التيك أواي")
                        .FontSize(10).FontColor("#94a3b8");
                });
                row.ConstantItem(220).Column(c =>
                {
                    c.Item().AlignRight().Text(_isEnglish
                        ? $"From: {_fromDate:dd/MM/yyyy}  To: {_toDate:dd/MM/yyyy}"
                        : $"من: {_fromDate:dd/MM/yyyy}  إلى: {_toDate:dd/MM/yyyy}")
                        .FontSize(8).FontColor(Colors.White);
                    c.Item().AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                        .FontSize(8).FontColor("#94a3b8");
                });
            });
            col.Item().Height(1).Background("#e2e8f0");
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Row(row =>
        {
            row.RelativeItem().Text(_isEnglish ? "TakeAway Customers Report" : "تقرير عملاء التيك أواي")
                .FontSize(8).FontColor(Colors.Grey.Medium);
            row.ConstantItem(100).AlignRight().Text(txt =>
            {
                txt.Span(_isEnglish ? "Page " : "صفحة ").FontSize(8).FontColor(Colors.Grey.Medium);
                txt.CurrentPageNumber().FontSize(8).Bold();
                txt.Span(_isEnglish ? " of " : " من ").FontSize(8).FontColor(Colors.Grey.Medium);
                txt.TotalPages().FontSize(8).Bold();
            });
        });
    }

    // ─── KPI Cards ────────────────────────────────────────────────────────────

    private void ComposeKpiCards(ColumnDescriptor col)
    {
        int totalCustomers = _customers.Count;
        int totalOrders = _customers.Sum(c => c.TotalOrders);
        decimal totalRevenue = _customers.Sum(c => c.TotalRevenue);
        decimal avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
        var topCustomer = _customers.OrderByDescending(c => c.TotalRevenue).FirstOrDefault();
        int registered = _customers.Count(c => c.IsRegistered);

        col.Item().PaddingBottom(10).Row(row =>
        {
            KpiCard(row.RelativeItem(),
                _isEnglish ? "Total Customers" : "إجمالي العملاء",
                $"{totalCustomers}",
                _isEnglish ? $"{registered} Registered" : $"{registered} مسجل",
                "#0f172a", "#e2e8f0");
            row.ConstantItem(8);
            KpiCard(row.RelativeItem(),
                _isEnglish ? "Total Orders" : "إجمالي الطلبات",
                $"{totalOrders}",
                _isEnglish ? $"{_customers.Sum(c => c.VoidedOrders)} Voided" : $"{_customers.Sum(c => c.VoidedOrders)} ملغي",
                "#166534", "#dcfce7");
            row.ConstantItem(8);
            KpiCard(row.RelativeItem(),
                _isEnglish ? "Total Revenue" : "إجمالي الإيرادات",
                $"{totalRevenue:0.##} {Currency}",
                _isEnglish ? "Completed orders only" : "الطلبات المكتملة فقط",
                "#1e3a8a", "#dbeafe");
            row.ConstantItem(8);
            KpiCard(row.RelativeItem(),
                _isEnglish ? "Avg Order Value" : "متوسط قيمة الطلب",
                $"{avgOrderValue:0.##} {Currency}",
                topCustomer != null ? (_isEnglish ? $"Top: {topCustomer.CustomerName}" : $"الأعلى: {topCustomer.CustomerName}") : "",
                "#7c2d12", "#ffedd5");
        });
    }

    private static void KpiCard(IContainer container, string label, string value, string sub, string bgColor, string lightBg)
    {
        container.Background(lightBg).Border(1).BorderColor(bgColor).Padding(10).Column(c =>
        {
            c.Item().Text(label).FontSize(8).FontColor(bgColor).Bold();
            c.Item().Text(value).FontSize(16).Bold().FontColor(bgColor);
            if (!string.IsNullOrEmpty(sub))
                c.Item().Text(sub).FontSize(7).FontColor(Colors.Grey.Darken1);
        });
    }

    // ─── Bar Chart ────────────────────────────────────────────────────────────

    private void ComposeBarChart(ColumnDescriptor col)
    {
        var top10 = _customers.OrderByDescending(c => c.TotalRevenue).Take(10).ToList();
        if (!top10.Any()) return;

        decimal maxRevenue = top10.Max(c => c.TotalRevenue);
        if (maxRevenue == 0) return;

        col.Item().PaddingBottom(8).Column(chartCol =>
        {
            chartCol.Item().PaddingBottom(6).Text(
                _isEnglish ? "Top Customers by Revenue" : "أعلى العملاء إيراداً")
                .FontSize(11).Bold().FontColor("#0f172a");

            foreach (var (c, i) in top10.Select((c, i) => (c, i)))
            {
                decimal ratio = maxRevenue > 0 ? c.TotalRevenue / maxRevenue : 0;
                string[] barColors = { "#0f172a", "#1e3a8a", "#166534", "#7c2d12", "#4c1d95",
                                       "#064e3b", "#7f1d1d", "#1e40af", "#14532d", "#78350f" };
                string barColor = barColors[i % barColors.Length];

                chartCol.Item().PaddingVertical(2).Row(barRow =>
                {
                    // Customer name label
                    barRow.ConstantItem(120).AlignMiddle().Text(
                        c.CustomerName.Length > 15 ? c.CustomerName.Substring(0, 15) + "…" : c.CustomerName)
                        .FontSize(8).FontColor("#334155");

                    // Bar container
                    barRow.RelativeItem().Height(16).Background("#f1f5f9").Column(bc =>
                    {
                        bc.Item().Row(br =>
                        {
                            // Filled portion
                            br.RelativeItem((float)ratio).Height(16).Background(barColor);
                            // Empty portion
                            if (ratio < 1)
                                br.RelativeItem((float)(1 - ratio)).Height(16);
                        });
                    });

                    // Value label
                    barRow.ConstantItem(80).AlignMiddle().AlignRight()
                        .Text($"{c.TotalRevenue:0.##} {Currency}").FontSize(8).Bold().FontColor(barColor);
                });
            }
        });
    }

    // ─── Summary Table ────────────────────────────────────────────────────────

    private void ComposeSummaryTable(ColumnDescriptor col)
    {
        col.Item().PaddingBottom(8).Column(tblCol =>
        {
            tblCol.Item().PaddingBottom(4).Text(
                _isEnglish ? "Customer Summary" : "ملخص العملاء")
                .FontSize(11).Bold().FontColor("#0f172a");

            tblCol.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(20);   // #
                    cols.RelativeColumn(3);    // Name
                    cols.RelativeColumn(2);    // Phone
                    cols.ConstantColumn(45);   // Orders
                    cols.RelativeColumn(2);    // Revenue
                    cols.RelativeColumn(2);    // Avg
                    cols.RelativeColumn(2);    // Last Order
                    cols.ConstantColumn(35);   // Status
                });

                // Header
                static void Hdr(IContainer c, string t) =>
                    c.Background("#0f172a").Padding(5).Text(t).FontSize(8).Bold().FontColor(Colors.White).AlignCenter();

                Hdr(table.Cell().Row(1).Column(1), "#");
                Hdr(table.Cell().Row(1).Column(2), _isEnglish ? "Customer" : "العميل");
                Hdr(table.Cell().Row(1).Column(3), _isEnglish ? "Phone" : "التليفون");
                Hdr(table.Cell().Row(1).Column(4), _isEnglish ? "Orders" : "الطلبات");
                Hdr(table.Cell().Row(1).Column(5), _isEnglish ? "Revenue" : "الإيرادات");
                Hdr(table.Cell().Row(1).Column(6), _isEnglish ? "Avg/Order" : "المتوسط");
                Hdr(table.Cell().Row(1).Column(7), _isEnglish ? "Last Order" : "آخر طلب");
                Hdr(table.Cell().Row(1).Column(8), _isEnglish ? "Status" : "الحالة");

                uint rowNum = 2;
                foreach (var (c, idx) in _customers.Select((c, i) => (c, i)))
                {
                    string bg = idx % 2 == 0 ? Colors.White : "#f8fafc";
                    decimal avg = c.TotalOrders > 0 ? c.TotalRevenue / c.TotalOrders : 0;

                    static void Cell(IContainer ct, string t, string bg, bool bold = false, string? color = null)
                    {
                        var txt = ct.Background(bg).Padding(4).Text(t).FontSize(8).FontColor(color ?? Colors.Black);
                        if (bold) txt.Bold();
                    }

                    Cell(table.Cell().Row(rowNum).Column(1), $"{idx + 1}", bg, false, "#64748b");
                    Cell(table.Cell().Row(rowNum).Column(2), c.CustomerName, bg, true);
                    Cell(table.Cell().Row(rowNum).Column(3), c.CustomerPhone, bg);
                    Cell(table.Cell().Row(rowNum).Column(4), $"{c.TotalOrders}", bg, true, "#1e3a8a");
                    Cell(table.Cell().Row(rowNum).Column(5), $"{c.TotalRevenue:0.##}", bg, true, "#166534");
                    Cell(table.Cell().Row(rowNum).Column(6), $"{avg:0.##}", bg);
                    Cell(table.Cell().Row(rowNum).Column(7), c.LastOrderDate?.ToString("dd/MM/yy") ?? "-", bg);

                    string statusText = c.IsRegistered ? (_isEnglish ? "✓ Reg" : "✓ مسجل") : (_isEnglish ? "Guest" : "زائر");
                    string statusColor = c.IsRegistered ? "#166534" : "#92400e";
                    Cell(table.Cell().Row(rowNum).Column(8), statusText, bg, false, statusColor);

                    rowNum++;
                }
            });
        });
    }

    // ─── Per-Customer Detail ──────────────────────────────────────────────────

    private void ComposeCustomerDetails(ColumnDescriptor col)
    {
        col.Item().PaddingBottom(4).Text(
            _isEnglish ? "Order Details per Customer" : "تفاصيل الطلبات لكل عميل")
            .FontSize(11).Bold().FontColor("#0f172a");

        foreach (var customer in _customers)
        {
            if (!customer.Orders.Any()) continue;

            col.Item().PaddingBottom(10).Border(1).BorderColor("#e2e8f0").Column(custCol =>
            {
                // Customer header bar
                custCol.Item().Background(customer.IsRegistered ? "#0f172a" : "#78350f")
                    .Padding(8).Row(hRow =>
                    {
                        hRow.RelativeItem().Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.AutoItem().Text(customer.CustomerName).FontSize(10).Bold().FontColor(Colors.White);
                                if (customer.IsRegistered)
                                    r.AutoItem().PaddingLeft(6).Text("★").FontSize(8).FontColor("#fbbf24");
                            });
                            c.Item().Text(customer.CustomerPhone).FontSize(8).FontColor("#94a3b8");
                        });
                        hRow.ConstantItem(200).Column(c =>
                        {
                            decimal avg = customer.TotalOrders > 0 ? customer.TotalRevenue / customer.TotalOrders : 0;
                            c.Item().AlignRight().Text($"{(_isEnglish ? "Orders: " : "الطلبات: ")}{customer.TotalOrders}  |  {(_isEnglish ? "Revenue: " : "الإيراد: ")}{customer.TotalRevenue:0.##} {Currency}")
                                .FontSize(8).FontColor(Colors.White);
                            c.Item().AlignRight().Text($"{(_isEnglish ? "Avg: " : "المتوسط: ")}{avg:0.##} {Currency}  |  {(_isEnglish ? "Since: " : "منذ: ")}{customer.FirstOrderDate?.ToString("dd/MM/yyyy") ?? "-"}")
                                .FontSize(7).FontColor("#94a3b8");
                        });
                    });

                // Orders table
                custCol.Item().Padding(6).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(45);   // Order#
                        cols.ConstantColumn(70);   // Date
                        cols.ConstantColumn(55);   // Time
                        cols.RelativeColumn(2);    // Amount
                        cols.RelativeColumn(2);    // Status
                        cols.RelativeColumn(3);    // Cashier
                        cols.RelativeColumn(3);    // Address/Note
                    });

                    // Mini header
                    static void MHdr(IContainer c, string t) =>
                        c.Background("#f1f5f9").BorderBottom(1).BorderColor("#e2e8f0")
                         .Padding(3).Text(t).FontSize(7).Bold().FontColor("#475569");

                    MHdr(table.Cell().Row(1).Column(1), _isEnglish ? "Order#" : "رقم الطلب");
                    MHdr(table.Cell().Row(1).Column(2), _isEnglish ? "Date" : "التاريخ");
                    MHdr(table.Cell().Row(1).Column(3), _isEnglish ? "Time" : "الوقت");
                    MHdr(table.Cell().Row(1).Column(4), _isEnglish ? "Amount" : "المبلغ");
                    MHdr(table.Cell().Row(1).Column(5), _isEnglish ? "Status" : "الحالة");
                    MHdr(table.Cell().Row(1).Column(6), _isEnglish ? "Cashier" : "الكاشير");
                    MHdr(table.Cell().Row(1).Column(7), _isEnglish ? "Address / Note" : "العنوان / الملاحظة");

                    uint rowNum = 2;
                    foreach (var (order, idx) in customer.Orders.Select((o, i) => (o, i)))
                    {
                        string bg = idx % 2 == 0 ? Colors.White : "#fafafa";
                        bool isVoided = order.State?.ToLower() == "voided";
                        string amtColor = isVoided ? "#ef4444" : "#166534";
                        string statusColor = isVoided ? "#ef4444" : "#166534";
                        string statusText = isVoided
                            ? (_isEnglish ? "Voided" : "ملغي")
                            : (_isEnglish ? "Completed" : "مكتمل");

                        string addrNote = string.Join(" | ", new[] { order.Address, order.Note }
                            .Where(s => !string.IsNullOrWhiteSpace(s)));

                        static void OCell(IContainer ct, string t, string bg, string? color = null, bool bold = false)
                        {
                            var txt = ct.Background(bg).Padding(3).Text(t).FontSize(7).FontColor(color ?? Colors.Black);
                            if (bold) txt.Bold();
                        }

                        OCell(table.Cell().Row(rowNum).Column(1), $"#{order.OrderId}", bg, "#1e3a8a", true);
                        OCell(table.Cell().Row(rowNum).Column(2), order.OrderDate?.ToString("dd/MM/yyyy") ?? "-", bg);
                        OCell(table.Cell().Row(rowNum).Column(3), order.OrderDate?.ToString("hh:mm tt") ?? "-", bg);
                        OCell(table.Cell().Row(rowNum).Column(4), $"{order.GrandTotal:0.##} {Currency}", bg, amtColor, true);
                        OCell(table.Cell().Row(rowNum).Column(5), statusText, bg, statusColor, true);
                        OCell(table.Cell().Row(rowNum).Column(6), order.CashierName, bg);
                        OCell(table.Cell().Row(rowNum).Column(7), addrNote, bg, "#64748b");

                        rowNum++;

                        if (order.Items != null && order.Items.Any())
                        {
                            var itemsStr = string.Join("  •  ", order.Items.Select(item =>
                            {
                                var attrStr = item.Attributes.Any() ? $" ({string.Join(", ", item.Attributes)})" : "";
                                return $"{item.Quantity:0.##}x {item.ItemName}{attrStr} [{item.TotalAmount:0.##} {Currency}]";
                            }));

                            table.Cell().Row(rowNum).Column(1).ColumnSpan(7).Background(bg).PaddingHorizontal(8).PaddingBottom(4).Text(txt =>
                            {
                                txt.Span(_isEnglish ? "Items & Details: " : "الأصناف والإضافات: ").FontSize(6.5f).Bold().FontColor("#475569");
                                txt.Span(itemsStr).FontSize(6.5f).FontColor("#1e293b");
                            });

                            rowNum++;
                        }
                    }
                });
            });
        }
    }

    // ─── Thermal ──────────────────────────────────────────────────────────────

    private void ComposeThermal(ColumnDescriptor col)
    {
        col.Item().AlignCenter().Text(_isEnglish ? "TakeAway Customers" : "عملاء التيك أواي")
            .FontSize(10).Bold();
        col.Item().AlignCenter().Text(_branchName).FontSize(8);
        col.Item().AlignCenter().Text($"{_fromDate:dd/MM/yy} - {_toDate:dd/MM/yy}").FontSize(7);
        col.Item().Height(1).Background(Colors.Black);
        col.Item().PaddingVertical(2);

        foreach (var c in _customers)
        {
            col.Item().PaddingVertical(2).Column(cc =>
            {
                cc.Item().Row(r =>
                {
                    r.RelativeItem().Text(c.CustomerName).FontSize(8).Bold();
                    r.AutoItem().Text(c.CustomerPhone).FontSize(7).FontColor(Colors.Grey.Darken1);
                });
                cc.Item().Text($"  {(_isEnglish ? "Orders:" : "طلبات:")} {c.TotalOrders}  |  {(_isEnglish ? "Total:" : "الإجمالي:")} {c.TotalRevenue:0.##} {Currency}")
                    .FontSize(7);
            });
            col.Item().Height(0.5f).Background(Colors.Grey.Lighten2);
        }

        col.Item().Height(1).Background(Colors.Black);
        col.Item().PaddingVertical(3).AlignCenter()
            .Text($"{(_isEnglish ? "Total Customers:" : "إجمالي العملاء:")} {_customers.Count}  |  {(_isEnglish ? "Revenue:" : "الإيرادات:")} {_customers.Sum(c => c.TotalRevenue):0.##} {Currency}")
            .FontSize(8).Bold();
    }
}
