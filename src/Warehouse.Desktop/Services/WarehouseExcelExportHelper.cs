using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;

namespace Warehouse.Desktop.Services;

/// <summary>
/// Professional Excel (.xlsx) generator using ClosedXML with Arabic RTL styling.
/// Supports dynamic currency and auto-sized column widths.
/// </summary>
public static class WarehouseExcelExportHelper
{
    private static readonly XLColor PrimaryHeaderBg = XLColor.FromHtml("#1E3A5F");
    private static readonly XLColor SecondaryHeaderBg = XLColor.FromHtml("#2A5298");
    private static readonly XLColor TableHeaderBg = XLColor.FromHtml("#0F172A");
    private static readonly XLColor TotalRowBg = XLColor.FromHtml("#F1F5F9");
    private static readonly XLColor BorderColor = XLColor.FromHtml("#CBD5E1");
    private static readonly XLColor AltRowBg = XLColor.FromHtml("#F8FAFC");

    private static IXLWorksheet CreateStyledSheet(XLWorkbook wb, string sheetName, string reportTitle, string subtitle)
    {
        var ws = wb.Worksheets.Add(sheetName);
        ws.RightToLeft = true;

        // Title row
        ws.Cell(1, 1).Value = reportTitle;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = PrimaryHeaderBg;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(1).Height = 35;

        // Subtitle row
        ws.Cell(2, 1).Value = $"{subtitle} | تاريخ الاستخراج: {DateTime.Now:yyyy/MM/dd hh:mm tt}";
        ws.Cell(2, 1).Style.Font.FontSize = 10;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");
        ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(2).Height = 20;

        return ws;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 1. تقييم المخزون (Stock Valuation)
    // ──────────────────────────────────────────────────────────────────────────
    public static byte[] GenerateStockValuation(
        IEnumerable<dynamic> items,
        decimal totalValue,
        int totalItemsCount,
        decimal avgCost,
        string warehouseFilter,
        string categoryFilter,
        DateTime? asOfDate,
        string currency = "ج.م",
        decimal totalQuantity = 0,
        decimal avgCapitalPerItem = 0)
    {
        using var wb = new XLWorkbook();
        var subtitle = $"المخزن: {warehouseFilter} | الفئة: {categoryFilter} | حتى تاريخ: {asOfDate?.ToString("yyyy/MM/dd") ?? "الكل"}";
        var ws = CreateStyledSheet(wb, "تقييم المخزون", "تقرير تقييم المخزون (بالتكلفة المرجحة)", subtitle);

        ws.Range(1, 1, 1, 9).Merge();
        ws.Range(2, 1, 2, 9).Merge();

        // KPI Box
        int row = 4;
        ws.Cell(row, 2).Value = $"إجمالي رأس المال ({currency}):";
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 3).Value = totalValue;
        ws.Cell(row, 3).Style.NumberFormat.Format = $"#,##0.00 {currency}";
        ws.Cell(row, 3).Style.Font.Bold = true;

        ws.Cell(row, 4).Value = "عدد الأصناف:";
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 5).Value = totalItemsCount;
        ws.Cell(row, 5).Style.Font.Bold = true;

        ws.Cell(row, 6).Value = "إجمالي الكمية:";
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 7).Value = totalQuantity;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 7).Style.Font.Bold = true;

        ws.Cell(row, 8).Value = $"متوسط رأس مال الصنف ({currency}):";
        ws.Cell(row, 8).Style.Font.Bold = true;
        ws.Cell(row, 9).Value = avgCapitalPerItem > 0 ? avgCapitalPerItem : (totalItemsCount > 0 ? totalValue / totalItemsCount : 0);
        ws.Cell(row, 9).Style.NumberFormat.Format = $"#,##0.00 {currency}";
        ws.Cell(row, 9).Style.Font.Bold = true;

        ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
        ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(row, 1, row, 9).Style.Border.OutsideBorderColor = XLColor.FromHtml("#93C5FD");
        row += 2;

        // Headers
        string[] headers = { "#", "كود الصنف (SKU)", "اسم الصنف", "الفئة", "المخزن", "الكمية المتاحة", "الوحدة", $"متوسط التكلفة ({currency})", $"إجمالي القيمة ({currency})" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = TableHeaderBg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws.Row(row).Height = 24;

        int index = 1;
        foreach (var item in items)
        {
            row++;
            ws.Cell(row, 1).Value = index++;
            ws.Cell(row, 2).Value = (string)(item.SKU ?? "");
            ws.Cell(row, 3).Value = (string)(item.ProductName ?? "");
            ws.Cell(row, 4).Value = (string)(item.CategoryName ?? "");
            ws.Cell(row, 5).Value = (string)(item.WarehouseName ?? "");
            ws.Cell(row, 6).Value = (decimal)item.CurrentStock;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = (string)(item.UnitName ?? "");
            ws.Cell(row, 8).Value = (decimal)item.AverageCost;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Value = (decimal)item.TotalValue;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";

            if (index % 2 == 0)
                ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = AltRowBg;
        }

        // Totals row
        row++;
        ws.Cell(row, 1).Value = "الإجمالي";
        ws.Range(row, 1, row, 8).Merge();
        ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 9).Value = totalValue;
        ws.Cell(row, 9).Style.NumberFormat.Format = $"#,##0.00 {currency}";
        ws.Cell(row, 9).Style.Font.Bold = true;
        ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = TotalRowBg;
        ws.Range(row, 1, row, 9).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        ws.Range(row, 1, row, 9).Style.Border.BottomBorder = XLBorderStyleValues.Double;

        // Set generous column widths so no text is truncated
        ws.Column(1).Width = 8;
        ws.Column(2).Width = 22;
        ws.Column(3).Width = 35;
        ws.Column(4).Width = 20;
        ws.Column(5).Width = 25;
        ws.Column(6).Width = 16;
        ws.Column(7).Width = 14;
        ws.Column(8).Width = 20;
        ws.Column(9).Width = 22;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 2. كارت الصنف وحركة المخزون (Item Movement Ledger)
    // ──────────────────────────────────────────────────────────────────────────
    public static byte[] GenerateItemMovements(
        IEnumerable<dynamic> items,
        string productName,
        string sku,
        decimal totalIn,
        decimal totalOut,
        decimal netChange,
        decimal closingBalance,
        DateTime fromDate,
        DateTime toDate,
        string currency = "ج.م")
    {
        using var wb = new XLWorkbook();
        var subtitle = $"الصنف: {productName} ({sku}) | الفترة: من {fromDate:yyyy/MM/dd} إلى {toDate:yyyy/MM/dd}";
        var ws = CreateStyledSheet(wb, "حركة الصنف", "كارت الصنف وسجل حركة المخزون", subtitle);

        ws.Range(1, 1, 1, 10).Merge();
        ws.Range(2, 1, 2, 10).Merge();

        // KPI Box
        int row = 4;
        ws.Cell(row, 1).Value = "إجمالي الوارد (+):";
        ws.Cell(row, 2).Value = totalIn;
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Font.FontColor = XLColor.FromHtml("#16A34A");

        ws.Cell(row, 3).Value = "إجمالي المنصرف (-):";
        ws.Cell(row, 4).Value = totalOut;
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 4).Style.Font.FontColor = XLColor.FromHtml("#DC2626");

        ws.Cell(row, 6).Value = "صافي الحركة:";
        ws.Cell(row, 7).Value = netChange;
        ws.Cell(row, 7).Style.Font.Bold = true;

        ws.Cell(row, 9).Value = "الرصيد الختامي:";
        ws.Cell(row, 10).Value = closingBalance;
        ws.Cell(row, 10).Style.Font.Bold = true;
        ws.Cell(row, 10).Style.Font.FontColor = XLColor.FromHtml("#2563EB");

        ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
        ws.Range(row, 1, row, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        row += 2;

        string[] headers = { "#", "التاريخ والوقت", "نوع الحركة", "المستند المرجعي", "المستودع", "وارد (+)", "صادر (-)", "الرصيد التراكمي", $"تكلفة الوحدة ({currency})", $"إجمالي القيمة ({currency})" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = TableHeaderBg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws.Row(row).Height = 24;

        int index = 1;
        foreach (var item in items)
        {
            row++;
            ws.Cell(row, 1).Value = index++;
            ws.Cell(row, 2).Value = item.MovementDate != null ? ((DateTime)item.MovementDate).ToString("yyyy/MM/dd hh:mm tt") : "";
            ws.Cell(row, 3).Value = (string)(item.MovementTypeAr ?? item.MovementType ?? "");
            ws.Cell(row, 4).Value = (string)(item.ReferenceNumber ?? item.Reference ?? "");
            ws.Cell(row, 5).Value = (string)(item.WarehouseName ?? "");
            ws.Cell(row, 6).Value = (decimal)item.QuantityIn;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = (decimal)item.QuantityOut;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 8).Value = (decimal)item.RunningBalance;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Value = (decimal)item.UnitCost;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 10).Value = (decimal)item.TotalCost;
            ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";

            if (index % 2 == 0)
                ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = AltRowBg;
        }

        ws.Column(1).Width = 8;
        ws.Column(2).Width = 18;
        ws.Column(3).Width = 22;
        ws.Column(4).Width = 24;
        ws.Column(5).Width = 22;
        ws.Column(6).Width = 14;
        ws.Column(7).Width = 14;
        ws.Column(8).Width = 16;
        ws.Column(9).Width = 18;
        ws.Column(10).Width = 20;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 3. نواقص المخزون وحد إعادة الطلب (Low Stock & Reorder Alerts)
    // ──────────────────────────────────────────────────────────────────────────
    public static byte[] GenerateLowStock(IEnumerable<dynamic> items, int outOfStock, int critical, int low, string currency = "ج.م")
    {
        using var wb = new XLWorkbook();
        var subtitle = $"أصناف نافدة: {outOfStock} | حرجة: {critical} | منخفضة: {low}";
        var ws = CreateStyledSheet(wb, "نواقص المخزون", "تقرير نواقص المخزون وحد إعادة الطلب", subtitle);

        ws.Range(1, 1, 1, 9).Merge();
        ws.Range(2, 1, 2, 9).Merge();

        int row = 4;
        string[] headers = { "#", "كود الصنف", "اسم الصنف", "المستودع", "الرصيد الحالي", "حد إعادة الطلب", "العجز المطلوب", "الكمية المقترحة", $"آخر سعر شراء ({currency})" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = TableHeaderBg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws.Row(row).Height = 24;

        int index = 1;
        foreach (var item in items)
        {
            row++;
            ws.Cell(row, 1).Value = index++;
            ws.Cell(row, 2).Value = (string)(item.SKU ?? "");
            ws.Cell(row, 3).Value = (string)(item.ProductName ?? "");
            ws.Cell(row, 4).Value = (string)(item.WarehouseName ?? "");
            ws.Cell(row, 5).Value = (decimal)item.CurrentStock;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Value = (decimal)item.ReorderLevel;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = (decimal)(item.Shortage ?? 0m);
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 8).Value = (decimal)item.SuggestedOrderQty;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Value = (decimal)(item.LastPrice ?? 0m);
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";

            if (index % 2 == 0)
                ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = AltRowBg;
        }

        ws.Column(1).Width = 8;
        ws.Column(2).Width = 20;
        ws.Column(3).Width = 35;
        ws.Column(4).Width = 22;
        ws.Column(5).Width = 16;
        ws.Column(6).Width = 16;
        ws.Column(7).Width = 16;
        ws.Column(8).Width = 18;
        ws.Column(9).Width = 20;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 4. تقرير ديون ومستحقات الموردين (Supplier Aging)
    // ──────────────────────────────────────────────────────────────────────────
    public static byte[] GenerateSupplierAging(
        IEnumerable<dynamic> items,
        decimal totalDebt,
        decimal current30,
        decimal days31To60,
        decimal days61To90,
        decimal over90,
        string currency = "ج.م")
    {
        using var wb = new XLWorkbook();
        var subtitle = $"إجمالي الديون: {totalDebt:N2} {currency} | جارية (<30): {current30:N2} | متأخرة (31-60): {days31To60:N2} | متأخرة (61-90): {days61To90:N2} | حرجة (>90): {over90:N2}";
        var ws = CreateStyledSheet(wb, "أعمار ديون الموردين", "تقرير ديون ومستحقات الموردين (Supplier Aging)", subtitle);

        ws.Range(1, 1, 1, 9).Merge();
        ws.Range(2, 1, 2, 9).Merge();

        int row = 4;
        string[] headers = { "#", "المورد", "رقم الفاتورة", "تاريخ الفاتورة", "تاريخ الاستحقاق", "أيام التأخير", $"إجمالي الفاتورة ({currency})", $"المسدد ({currency})", $"المتبقي المستحق ({currency})" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = TableHeaderBg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws.Row(row).Height = 24;

        int index = 1;
        foreach (var item in items)
        {
            row++;
            ws.Cell(row, 1).Value = index++;
            ws.Cell(row, 2).Value = (string)(item.SupplierName ?? "");
            ws.Cell(row, 3).Value = (string)(item.InvoiceNumber ?? "");
            ws.Cell(row, 4).Value = item.InvoiceDate != null ? ((DateTime)item.InvoiceDate).ToString("yyyy/MM/dd") : "";
            ws.Cell(row, 5).Value = item.DueDate != null ? ((DateTime)item.DueDate).ToString("yyyy/MM/dd") : "";
            ws.Cell(row, 6).Value = (int)item.DaysOverdue;
            ws.Cell(row, 7).Value = (decimal)item.TotalAmount;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 8).Value = (decimal)item.PaidAmount;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Value = (decimal)item.RemainingAmount;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";

            if (index % 2 == 0)
                ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = AltRowBg;
        }

        row++;
        ws.Cell(row, 1).Value = "الإجمالي المستحق";
        ws.Range(row, 1, row, 8).Merge();
        ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 9).Value = totalDebt;
        ws.Cell(row, 9).Style.NumberFormat.Format = $"#,##0.00 {currency}";
        ws.Cell(row, 9).Style.Font.Bold = true;
        ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = TotalRowBg;

        ws.Column(1).Width = 8;
        ws.Column(2).Width = 30;
        ws.Column(3).Width = 20;
        ws.Column(4).Width = 16;
        ws.Column(5).Width = 16;
        ws.Column(6).Width = 14;
        ws.Column(7).Width = 20;
        ws.Column(8).Width = 20;
        ws.Column(9).Width = 22;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 5. أرصدة المخزون (Stock Balances)
    // ──────────────────────────────────────────────────────────────────────────
    public static byte[] GenerateStockBalances(
        IEnumerable<dynamic> items,
        int totalItems,
        decimal totalOnHand,
        decimal totalAvailable,
        int lowStockCount,
        string warehouseFilter)
    {
        using var wb = new XLWorkbook();
        var subtitle = $"المستودع: {warehouseFilter} | إجمالي الأصناف: {totalItems} | أصناف منخفضة: {lowStockCount}";
        var ws = CreateStyledSheet(wb, "أرصدة المخزون", "تقرير أرصدة المخزون وحالة التوفر", subtitle);

        ws.Range(1, 1, 1, 9).Merge();
        ws.Range(2, 1, 2, 9).Merge();

        // KPI Box
        int row = 4;
        ws.Cell(row, 1).Value = "إجمالي الأصناف:";
        ws.Cell(row, 2).Value = totalItems;
        ws.Cell(row, 2).Style.Font.Bold = true;

        ws.Cell(row, 3).Value = "إجمالي الفعلي:";
        ws.Cell(row, 4).Value = totalOnHand;
        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 4).Style.Font.Bold = true;

        ws.Cell(row, 6).Value = "إجمالي المتاح:";
        ws.Cell(row, 7).Value = totalAvailable;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 7).Style.Font.Bold = true;
        ws.Cell(row, 7).Style.Font.FontColor = XLColor.FromHtml("#16A34A");

        ws.Cell(row, 8).Value = "تنبيهات العجز:";
        ws.Cell(row, 9).Value = lowStockCount;
        ws.Cell(row, 9).Style.Font.Bold = true;
        ws.Cell(row, 9).Style.Font.FontColor = XLColor.FromHtml("#DC2626");

        ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
        ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        row += 2;

        string[] headers = { "#", "كود المنتج", "اسم المنتج", "المستودع", "الموقع", "الكمية الفعلية", "الكمية المحجوزة", "الكمية المتاحة", "الوحدة" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = TableHeaderBg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws.Row(row).Height = 24;

        int index = 1;
        foreach (var item in items)
        {
            row++;
            ws.Cell(row, 1).Value = index++;
            ws.Cell(row, 2).Value = (string)(item.ProductCode ?? "");
            ws.Cell(row, 3).Value = (string)(item.ProductName ?? "");
            ws.Cell(row, 4).Value = (string)(item.WarehouseName ?? "");
            ws.Cell(row, 5).Value = (string)(item.LocationName ?? "");
            ws.Cell(row, 6).Value = (decimal)item.OnHand;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = (decimal)item.Reserved;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 8).Value = (decimal)item.Available;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Value = (string)(item.Unit ?? "");

            if (index % 2 == 0)
                ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = AltRowBg;
        }

        ws.Column(1).Width = 8;
        ws.Column(2).Width = 20;
        ws.Column(3).Width = 35;
        ws.Column(4).Width = 24;
        ws.Column(5).Width = 18;
        ws.Column(6).Width = 16;
        ws.Column(7).Width = 16;
        ws.Column(8).Width = 16;
        ws.Column(9).Width = 14;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 6. طلبيات الفروع (Branch Requisitions)
    // ──────────────────────────────────────────────────────────────────────────
    public static byte[] GenerateRequisitions(IEnumerable<dynamic> items, string filterStatus)
    {
        using var wb = new XLWorkbook();
        var subtitle = $"حالة التصفية: {filterStatus}";
        var ws = CreateStyledSheet(wb, "طلبيات الفروع", "سجل طلبيات التوريد من الفروع", subtitle);

        ws.Range(1, 1, 1, 6).Merge();
        ws.Range(2, 1, 2, 6).Merge();

        int row = 4;
        string[] headers = { "#", "رقم الطلب", "الفرع الطالب", "المخزن المستهدف", "تاريخ الطلب", "الحالة" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = TableHeaderBg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws.Row(row).Height = 24;

        int index = 1;
        foreach (var item in items)
        {
            row++;
            ws.Cell(row, 1).Value = index++;
            ws.Cell(row, 2).Value = (string)(item.RequisitionNumber ?? "");
            ws.Cell(row, 3).Value = (string)(item.BranchName ?? "");
            ws.Cell(row, 4).Value = (string)(item.WarehouseName ?? "");
            ws.Cell(row, 5).Value = item.RequestDate != null ? ((DateTime)item.RequestDate).ToString("yyyy/MM/dd hh:mm tt") : "";
            ws.Cell(row, 6).Value = (string)(item.StatusLabel ?? "");

            if (index % 2 == 0)
                ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = AltRowBg;
        }

        ws.Column(1).Width = 8;
        ws.Column(2).Width = 24;
        ws.Column(3).Width = 28;
        ws.Column(4).Width = 28;
        ws.Column(5).Width = 22;
        ws.Column(6).Width = 18;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 7. تفاصيل طلبية معينة (Single Requisition Detail)
    // ──────────────────────────────────────────────────────────────────────────
    public static byte[] GenerateRequisitionDetails(
        string reqNumber,
        string branchName,
        string warehouseName,
        DateTime requestDate,
        string statusLabel,
        IEnumerable<dynamic> lines)
    {
        using var wb = new XLWorkbook();
        var subtitle = $"الفرع: {branchName} | المخزن المورد: {warehouseName} | الحالة: {statusLabel}";
        var ws = CreateStyledSheet(wb, $"طلب_{reqNumber}", $"إذن طلبية توريد - {reqNumber}", subtitle);

        ws.Range(1, 1, 1, 7).Merge();
        ws.Range(2, 1, 2, 7).Merge();

        int row = 4;
        string[] headers = { "#", "اسم الصنف", "الوحدة", "الكمية المطلوبة", "الكمية المعتمدة", "المشحونة", "المستلمة" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = TableHeaderBg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws.Row(row).Height = 24;

        int index = 1;
        foreach (var line in lines)
        {
            row++;
            ws.Cell(row, 1).Value = index++;
            ws.Cell(row, 2).Value = (string)(line.ProductName ?? "");
            ws.Cell(row, 3).Value = (string)(line.UnitName ?? "");
            ws.Cell(row, 4).Value = (decimal)line.RequestedQty;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Value = (decimal)(line.ApprovedQty ?? 0m);
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Value = (decimal)(line.DispatchedQty ?? 0m);
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = (decimal)(line.ReceivedQty ?? 0m);
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

            if (index % 2 == 0)
                ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = AltRowBg;
        }

        ws.Column(1).Width = 8;
        ws.Column(2).Width = 35;
        ws.Column(3).Width = 14;
        ws.Column(4).Width = 18;
        ws.Column(5).Width = 18;
        ws.Column(6).Width = 18;
        ws.Column(7).Width = 18;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 8. تقرير مقارنة وتحليل طلبيات الفروع والمستودعات
    // ──────────────────────────────────────────────────────────────────────────
    public static byte[] GenerateBranchRequisitionsReport(
        string branchFilter,
        string warehouseFilter,
        string statusFilter,
        DateTime? fromDate,
        DateTime? toDate,
        IEnumerable<dynamic> branchComparisons,
        IEnumerable<dynamic> requisitionDetails)
    {
        using var wb = new XLWorkbook();
        var subtitle = $"الفرع: {branchFilter} | المستودع: {warehouseFilter} | الحالة: {statusFilter} | الفترة: {fromDate:yyyy/MM/dd} إلى {toDate:yyyy/MM/dd}";

        // ══════════════════ SHEET 1: مقارنة الفروع ══════════════════
        var ws1 = CreateStyledSheet(wb, "مقارنة الفروع", "تقرير مقارنة طلبيات الفروع والمستودعات", subtitle);
        ws1.Range(1, 1, 1, 8).Merge();
        ws1.Range(2, 1, 2, 8).Merge();

        int row1 = 4;
        string[] headers1 = { "#", "اسم الفرع", "إجمالي الطلبيات", "الطلبيات المستلمة", "قيد التجهيز / المراجعة", "إجمالي المطلوب", "إجمالي المورد", "نسبة الإنجاز" };
        for (int i = 0; i < headers1.Length; i++)
        {
            var cell = ws1.Cell(row1, i + 1);
            cell.Value = headers1[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = TableHeaderBg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws1.Row(row1).Height = 25;

        int bIdx = 1;
        int totalOrdersAll = 0;
        int totalReceivedAll = 0;
        int totalPendingAll = 0;
        decimal totalReqQtyAll = 0;
        decimal totalRecQtyAll = 0;

        foreach (var b in branchComparisons)
        {
            row1++;
            ws1.Cell(row1, 1).Value = bIdx++;
            ws1.Cell(row1, 2).Value = (string)(b.BranchName ?? "");
            ws1.Cell(row1, 3).Value = (int)b.TotalOrders;
            ws1.Cell(row1, 4).Value = (int)b.ReceivedOrders;
            ws1.Cell(row1, 5).Value = (int)b.PendingOrders;
            ws1.Cell(row1, 6).Value = (decimal)b.TotalRequestedQty;
            ws1.Cell(row1, 6).Style.NumberFormat.Format = "#,##0.00";
            ws1.Cell(row1, 7).Value = (decimal)b.TotalDeliveredQty;
            ws1.Cell(row1, 7).Style.NumberFormat.Format = "#,##0.00";
            ws1.Cell(row1, 8).Value = $"{(decimal)b.FulfillmentRate:0.#}%";

            totalOrdersAll += (int)b.TotalOrders;
            totalReceivedAll += (int)b.ReceivedOrders;
            totalPendingAll += (int)b.PendingOrders;
            totalReqQtyAll += (decimal)b.TotalRequestedQty;
            totalRecQtyAll += (decimal)b.TotalDeliveredQty;

            if (bIdx % 2 == 0)
                ws1.Range(row1, 1, row1, 8).Style.Fill.BackgroundColor = AltRowBg;
        }

        // Total Summary Row
        row1++;
        ws1.Cell(row1, 1).Value = "";
        ws1.Cell(row1, 2).Value = "الإجمالي العام لجميع الفروع";
        ws1.Cell(row1, 3).Value = totalOrdersAll;
        ws1.Cell(row1, 4).Value = totalReceivedAll;
        ws1.Cell(row1, 5).Value = totalPendingAll;
        ws1.Cell(row1, 6).Value = totalReqQtyAll;
        ws1.Cell(row1, 6).Style.NumberFormat.Format = "#,##0.00";
        ws1.Cell(row1, 7).Value = totalRecQtyAll;
        ws1.Cell(row1, 7).Style.NumberFormat.Format = "#,##0.00";
        var grandRate = totalReqQtyAll > 0 ? (totalRecQtyAll / totalReqQtyAll * 100m) : 0m;
        ws1.Cell(row1, 8).Value = $"{grandRate:0.#}%";

        var grandTotalRange1 = ws1.Range(row1, 1, row1, 8);
        grandTotalRange1.Style.Font.Bold = true;
        grandTotalRange1.Style.Fill.BackgroundColor = TotalRowBg;
        grandTotalRange1.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        grandTotalRange1.Style.Border.BottomBorder = XLBorderStyleValues.Double;

        ws1.Column(1).Width = 8;
        ws1.Column(2).Width = 28;
        ws1.Column(3).Width = 16;
        ws1.Column(4).Width = 16;
        ws1.Column(5).Width = 20;
        ws1.Column(6).Width = 18;
        ws1.Column(7).Width = 18;
        ws1.Column(8).Width = 16;

        // ══════════════════ SHEET 2: تفاصيل الطلبيات والأصناف ══════════════════
        var ws2 = CreateStyledSheet(wb, "تفاصيل الطلبيات", "تفاصيل بنود وحركات طلبيات الفروع", subtitle);
        ws2.Range(1, 1, 1, 11).Merge();
        ws2.Range(2, 1, 2, 11).Merge();

        int row2 = 4;
        string[] headers2 = { "#", "رقم الطلب", "الفرع الطالب", "المستودع المورد", "تاريخ الطلب", "كود الصنف", "اسم الصنف", "الوحدة", "المطلوب", "المعتمد / المستلم", "الحالة" };
        for (int i = 0; i < headers2.Length; i++)
        {
            var cell = ws2.Cell(row2, i + 1);
            cell.Value = headers2[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = TableHeaderBg;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws2.Row(row2).Height = 25;

        int rIdx = 1;
        foreach (var r in requisitionDetails)
        {
            row2++;
            ws2.Cell(row2, 1).Value = rIdx++;
            ws2.Cell(row2, 2).Value = (string)(r.RequisitionNumber ?? "");
            ws2.Cell(row2, 3).Value = (string)(r.BranchName ?? "");
            ws2.Cell(row2, 4).Value = (string)(r.WarehouseName ?? "");
            ws2.Cell(row2, 5).Value = r.RequestDate != null ? ((DateTime)r.RequestDate).ToString("yyyy/MM/dd hh:mm tt") : "";
            ws2.Cell(row2, 6).Value = (string)(r.ProductCode ?? "");
            ws2.Cell(row2, 7).Value = (string)(r.ProductName ?? "");
            ws2.Cell(row2, 8).Value = (string)(r.UnitName ?? "");
            ws2.Cell(row2, 9).Value = (decimal)r.RequestedQty;
            ws2.Cell(row2, 9).Style.NumberFormat.Format = "#,##0.00";
            ws2.Cell(row2, 10).Value = (decimal)r.DeliveredQty;
            ws2.Cell(row2, 10).Style.NumberFormat.Format = "#,##0.00";
            ws2.Cell(row2, 11).Value = (string)(r.StatusLabel ?? "");

            if (rIdx % 2 == 0)
                ws2.Range(row2, 1, row2, 11).Style.Fill.BackgroundColor = AltRowBg;
        }

        ws2.Column(1).Width = 8;
        ws2.Column(2).Width = 20;
        ws2.Column(3).Width = 22;
        ws2.Column(4).Width = 22;
        ws2.Column(5).Width = 22;
        ws2.Column(6).Width = 15;
        ws2.Column(7).Width = 30;
        ws2.Column(8).Width = 14;
        ws2.Column(9).Width = 16;
        ws2.Column(10).Width = 18;
        ws2.Column(11).Width = 18;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
