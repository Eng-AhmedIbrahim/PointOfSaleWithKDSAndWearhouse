using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;

namespace Warehouse.Desktop.Services;

/// <summary>
/// Professional Excel (.xlsx) generator using ClosedXML with Arabic RTL styling.
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
        DateTime? asOfDate)
    {
        using var wb = new XLWorkbook();
        var subtitle = $"المخزن: {warehouseFilter} | الفئة: {categoryFilter} | حتى تاريخ: {asOfDate?.ToString("yyyy/MM/dd") ?? "الكل"}";
        var ws = CreateStyledSheet(wb, "تقييم المخزون", "تقرير تقييم المخزون (بالتكلفة المرجحة)", subtitle);

        ws.Range(1, 1, 1, 9).Merge();
        ws.Range(2, 1, 2, 9).Merge();

        // KPI Box
        int row = 4;
        ws.Cell(row, 2).Value = "إجمالي قيمة المخزون:";
        ws.Cell(row, 3).Value = totalValue;
        ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 ج.م";
        ws.Cell(row, 3).Style.Font.Bold = true;

        ws.Cell(row, 5).Value = "عدد الأصناف:";
        ws.Cell(row, 6).Value = totalItemsCount;
        ws.Cell(row, 6).Style.Font.Bold = true;

        ws.Cell(row, 8).Value = "متوسط التكلفة:";
        ws.Cell(row, 9).Value = avgCost;
        ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00 ج.م";
        ws.Cell(row, 9).Style.Font.Bold = true;

        ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
        ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(row, 1, row, 9).Style.Border.OutsideBorderColor = XLColor.FromHtml("#93C5FD");
        row += 2;

        // Headers
        string[] headers = { "#", "كود الصنف (SKU)", "اسم الصنف", "الفئة", "المخزن", "الكمية المتاحة", "الوحدة", "متوسط التكلفة", "إجمالي القيمة" };
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
        int dataStartRow = row + 1;
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
        ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 9).Style.Font.Bold = true;
        ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = TotalRowBg;
        ws.Range(row, 1, row, 9).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        ws.Range(row, 1, row, 9).Style.Border.BottomBorder = XLBorderStyleValues.Double;

        ws.Columns(1, 9).AdjustToContents(10, 45);

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
        DateTime toDate)
    {
        using var wb = new XLWorkbook();
        var subtitle = $"الصنف: {productName} ({sku}) | الفترة: من {fromDate:yyyy/MM/dd} إلى {toDate:yyyy/MM/dd}";
        var ws = CreateStyledSheet(wb, "حركة الصنف", "كارت الصنف وسجل حركة المخزون", subtitle);

        ws.Range(1, 1, 1, 9).Merge();
        ws.Range(2, 1, 2, 9).Merge();

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

        ws.Cell(row, 5).Value = "صافي الحركة:";
        ws.Cell(row, 6).Value = netChange;
        ws.Cell(row, 6).Style.Font.Bold = true;

        ws.Cell(row, 7).Value = "الرصيد الختامي:";
        ws.Cell(row, 8).Value = closingBalance;
        ws.Cell(row, 8).Style.Font.Bold = true;
        ws.Cell(row, 8).Style.Font.FontColor = XLColor.FromHtml("#2563EB");

        ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
        ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        row += 2;

        string[] headers = { "#", "التاريخ والوقت", "نوع الحركة", "المستند المرجعي", "المستودع", "وارد (+)", "صادر (-)", "الرصيد التراكمي", "تكلفة الوحدة", "إجمالي القيمة" };
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
            ws.Cell(row, 2).Value = item.MovementDate != null ? ((DateTime)item.MovementDate).ToString("yyyy/MM/dd HH:mm") : "";
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

        ws.Columns(1, 10).AdjustToContents(10, 45);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 3. نواقص المخزون وحد إعادة الطلب (Low Stock & Reorder Alerts)
    // ──────────────────────────────────────────────────────────────────────────
    public static byte[] GenerateLowStock(IEnumerable<dynamic> items, int outOfStock, int critical, int low)
    {
        using var wb = new XLWorkbook();
        var subtitle = $"أصناف نافدة: {outOfStock} | حرجة: {critical} | منخفضة: {low}";
        var ws = CreateStyledSheet(wb, "نواقص المخزون", "تقرير نواقص المخزون وحد إعادة الطلب", subtitle);

        ws.Range(1, 1, 1, 8).Merge();
        ws.Range(2, 1, 2, 8).Merge();

        int row = 4;
        string[] headers = { "#", "كود الصنف", "اسم الصنف", "المستودع", "الرصيد الحالي", "حد إعادة الطلب", "الكمية المقترحة للطلب", "الحالة" };
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
            ws.Cell(row, 7).Value = (decimal)item.SuggestedOrderQty;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 8).Value = (string)(item.StatusAr ?? item.Status ?? "");

            if (index % 2 == 0)
                ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = AltRowBg;
        }

        ws.Columns(1, 8).AdjustToContents(10, 45);

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
        decimal over90)
    {
        using var wb = new XLWorkbook();
        var subtitle = $"إجمالي الديون: {totalDebt:N2} ج.م | جارية (<30): {current30:N2} | متأخرة (31-60): {days31To60:N2} | متأخرة (61-90): {days61To90:N2} | حرجة (>90): {over90:N2}";
        var ws = CreateStyledSheet(wb, "أعمار ديون الموردين", "تقرير ديون ومستحقات الموردين (Supplier Aging)", subtitle);

        ws.Range(1, 1, 1, 9).Merge();
        ws.Range(2, 1, 2, 9).Merge();

        int row = 4;
        string[] headers = { "#", "المورد", "رقم الفاتورة", "تاريخ الفاتورة", "تاريخ الاستحقاق", "أيام التأخير", "إجمالي الفاتورة", "المسدد", "المتبقي المستحق" };
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
        ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 9).Style.Font.Bold = true;
        ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = TotalRowBg;

        ws.Columns(1, 9).AdjustToContents(10, 45);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
