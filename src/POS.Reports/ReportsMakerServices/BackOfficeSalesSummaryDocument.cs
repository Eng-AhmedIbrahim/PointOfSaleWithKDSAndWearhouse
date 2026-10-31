namespace POS.Reports.ReportsMakerServices;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using POS.Contract.Dtos.ReportingDtos;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

public class BackOfficeSalesSummaryDocument : IDocument
{
    private readonly SalesSummaryDto _summary;
    private readonly List<SalesItemSummaryDto> _catItems;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly bool _isThermal;
    private readonly string _lang;
    private readonly bool _isEnglish;

    public BackOfficeSalesSummaryDocument(SalesSummaryDto summary, 
        List<SalesItemSummaryDto> catItems, 
        string branchName, DateTime fromDate, 
        DateTime toDate,
        bool isThermal = false, 
        string lang = "ar")
    {
        _summary = summary;
        _catItems = catItems;
        _branchName = branchName;
        _fromDate = fromDate;
        _toDate = toDate;
        _isThermal = isThermal;
        _lang = (lang ?? "ar").ToLower();
        _isEnglish = _lang == "en";
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
                col.Spacing(_isThermal ? 3 : 6);
                ComposeHeader(col);
                ComposeFinancialSummary(col);
                ComposeDetailedExpenses(col);
                ComposeSalesByType(col);
                ComposeEmployeeSales(col);
                ComposeCategorySales(col);
                ComposeVoidsAndDiscounts(col);
                ComposeTopItems(col);
                ComposeStaffOrders(col);
                ComposeHospitalityOrders(col);
                ComposeFooter(col);
            });
        });
    }

    private void ComposeSectionHeader(ColumnDescriptor col, string title)
    {
        col.Item().PaddingTop(5).Background(Colors.Grey.Lighten2).Border(1).Padding(2).AlignCenter().Text(title).Bold().FontSize(_isThermal ? 9 : 11);
    }

    private string GetLogoPath()
    {
        try
        {
            var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "ReceiptLogo");
            if (!Directory.Exists(folderPath))
                folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "ReceiptLogo");
            
            if (Directory.Exists(folderPath))
            {
                var file = Directory.GetFiles(folderPath, "*.*")
                    .FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                        f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                        f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));
                return file ?? "";
            }
        }
        catch { }
        return "";
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        // Branch Name (Black, Center)
        col.Item().AlignCenter().Text(_branchName).FontSize(_isThermal ? 11 : 14).Bold().FontColor(Colors.Black);
        
        // Report Title (Black, Center)
        col.Item().AlignCenter().Text(_isEnglish ? "Sales Summary Report" : "تقرير ملخص المبيعات").FontSize(_isThermal ? 12 : 18).Bold().FontColor(Colors.Black);

        // Subtitle
        col.Item().AlignCenter().Text(_isEnglish ? $"Period: {_fromDate:dd/MM/yyyy} - {_toDate:dd/MM/yyyy}" : $"الفترة من: {_fromDate:dd/MM/yyyy} إلى: {_toDate:dd/MM/yyyy}").FontSize(_isThermal ? 7 : 10).FontColor(Colors.Grey.Darken2);

        var logoPath = GetLogoPath();
        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
        {
            col.Item().PaddingTop(5).AlignCenter().Width(_isThermal ? 40 : 60).Image(logoPath).FitWidth();
        }
        
        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
    }

    private void ComposeFinancialSummary(ColumnDescriptor col)
    {
        ComposeSectionHeader(col, _isEnglish ? "Financial Summary" : "الملخص المالي العام");
        
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.RelativeColumn(4); cd.RelativeColumn(3); });
            
            AddSummaryRow(t, _isEnglish ? "Gross Sales" : "إجمالي المبيعات", _summary.Overall.TotalSales.ToString("N2"));
            AddSummaryRow(t, _isEnglish ? "Total Discounts" : "إجمالي الخصومات", _summary.Overall.TotalDiscount.ToString("N2"));
            AddSummaryRow(t, _isEnglish ? "Total Expenses" : "إجمالي المصروفات", _summary.Overall.Expenses.ToString("N2"));
            AddSummaryRow(t, _isEnglish ? "Total Refunds" : "إجمالي المرتجعات", _summary.Overall.RefundAmount.ToString("N2"));
            
            AddSummaryRow(t, _isEnglish ? "Workers Meals" : "وجبات العاملين", _summary.Staff.OrderCount.ToString());
            AddSummaryRow(t, _isEnglish ? "Hospitality" : "ضيافة", _summary.Hospitality.OrderCount.ToString());

            if ((_summary.Overall.Expenses) > 0)
            {
                AddSummaryRow(t, _isEnglish ? "Total Payouts/Expenses" : "إجمالي المصروفات", _summary.Overall.Expenses.ToString("N2"));
            }

            t.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter().Text(_isEnglish ? "Net Cash" : "صافي النقدية بالدرج").Bold();
            t.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter().Text(_summary.Overall.NetCash.ToString("N2")).Bold();
            
            t.Cell().Border(1).Background(Colors.Black).Padding(3).AlignCenter().Text(_isEnglish ? "Total Revenue" : "إجمالي الإيراد").Bold().FontColor(Colors.White);
            t.Cell().Border(1).Background(Colors.Black).Padding(3).AlignCenter().Text(_summary.Overall.TotalRevenue.ToString("N2")).Bold().FontColor(Colors.White);
        });
    }

    private void ComposeDetailedExpenses(ColumnDescriptor col)
    {
        if (_summary.DetailedExpenses == null || !_summary.DetailedExpenses.Any()) return;

        ComposeSectionHeader(col, _isEnglish ? "Detailed Expenses Log" : "بيان المصروفات التفصيلي");
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.RelativeColumn(1.5f); cd.RelativeColumn(3); cd.RelativeColumn(4); cd.RelativeColumn(2); });
            t.Header(h => {
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Cat" : "الفئة").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Spent By" : "بواسطة").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Description" : "البيان").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Amount" : "المبلغ").Bold();
            });

            foreach (var e in _summary.DetailedExpenses)
            {
                t.Cell().Border(1).Padding(1).AlignCenter().Text(e.Category);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(e.SpentBy ?? "-");
                t.Cell().Border(1).Padding(1).AlignRight().Text(e.Description);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(e.Amount.ToString("N2")).Bold();
            }
        });
    }

    private void AddSummaryRow(TableDescriptor t, string label, string value)
    {
        t.Cell().Border(1).Padding(2).AlignCenter().Text(label);
        t.Cell().Border(1).Padding(2).AlignCenter().Text(value).Bold();
    }

    private void ComposeSalesByType(ColumnDescriptor col)
    {
        ComposeSectionHeader(col, _isEnglish ? "Sales by Order Type" : "المبيعات حسب نوع الطلب");
        
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(1.2f); cd.RelativeColumn(2); cd.RelativeColumn(2); });
            t.Header(h => {
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Type" : "النوع").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Qty" : "عدد").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Disc" : "الخصم").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Total" : "الإجمالي").Bold();
            });
            
            foreach(var m in _summary.Overall.ModeDetails)
            {
                t.Cell().Border(1).Padding(1).AlignCenter().Text(m.ModeTitle);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(m.OrderCount.ToString());
                t.Cell().Border(1).Padding(1).AlignCenter().Text(m.Discount.ToString("N2"));
                t.Cell().Border(1).Padding(1).AlignCenter().Text(m.NetSales.ToString("N2")).Bold();
            }
        });
    }

    private void ComposeEmployeeSales(ColumnDescriptor col)
    {
        var hasSummaries = _summary.CashierSummaries != null && _summary.CashierSummaries.Any();
        var hasDetails = _summary.DetailedOrders != null && _summary.DetailedOrders.Any();
        
        if (!hasSummaries && !hasDetails) return;

        ComposeSectionHeader(col, _isEnglish ? "Staff Performance" : "أداء الموظفين");
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.RelativeColumn(4); cd.RelativeColumn(1); cd.RelativeColumn(2); cd.RelativeColumn(2); cd.RelativeColumn(2); });

            t.Header(h => {
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Staff" : "الموظف").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Qty" : "عدد").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Disc" : "خصم").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Void" : "فويد").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Total" : "إجمالي").Bold();
            });
            
            if (hasSummaries)
            {
                var cashiersOnly = _summary.CashierSummaries.Where(s => string.IsNullOrEmpty(s.Type) || s.Type.Equals("Cashier", StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var s in cashiersOnly)
                {
                    t.Cell().Border(1).Padding(1).AlignCenter().Text(s.Name);
                    t.Cell().Border(1).Padding(1).AlignCenter().Text(s.OrderCount.ToString());
                    t.Cell().Border(1).Padding(1).AlignCenter().Text(s.DiscountAmount.ToString("N2"));
                    t.Cell().Border(1).Padding(1).AlignCenter().Text(s.VoidAmount.ToString("N2"));
                    t.Cell().Border(1).Padding(1).AlignCenter().Text(s.TotalAmount.ToString("N2")).Bold();
                }
            }
            else
            {
                var empGroups = _summary.DetailedOrders.GroupBy(o => o.CashierName ?? "System")
                    .Select(g => new { 
                        Name = g.Key, 
                        Count = g.Count(),
                        Discount = g.Sum(o => o.TotalDiscount ?? 0), 
                        Void = g.Sum(o => o.VoidAmount ?? 0),
                        Total = g.Sum(o => o.GrandTotal ?? 0) 
                    });

                foreach (var eg in empGroups)
                {
                    t.Cell().Border(1).Padding(1).AlignCenter().Text(eg.Name);
                    t.Cell().Border(1).Padding(1).AlignCenter().Text(eg.Count.ToString());
                    t.Cell().Border(1).Padding(1).AlignCenter().Text(eg.Discount.ToString("N2"));
                    t.Cell().Border(1).Padding(1).AlignCenter().Text(eg.Void.ToString("N2"));
                    t.Cell().Border(1).Padding(1).AlignCenter().Text(eg.Total.ToString("N2")).Bold();
                }
            }
        });
    }

    private void ComposeCategorySales(ColumnDescriptor col)
    {
        ComposeSectionHeader(col, _isEnglish ? "Sales by Category" : "المبيعات حسب الفئات");
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.RelativeColumn(5); cd.RelativeColumn(3); cd.RelativeColumn(2); });
            t.Header(h => {
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Category" : "الفئة").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Total" : "الإجمالي").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text("%").Bold();
            });

            var categories = _catItems.GroupBy(ci => ci.CategoryName)
                .Select(g => new { Name = g.Key, Total = g.Sum(x => x.TotalAmount) })
                .OrderByDescending(x => x.Total);
            
            decimal grandTotal = categories.Sum(x => x.Total);

            foreach (var cat in categories)
            {
                t.Cell().Border(1).Padding(1).AlignCenter().Text(cat.Name).Bold();
                t.Cell().Border(1).Padding(1).AlignCenter().Text(cat.Total.ToString("N2"));
                t.Cell().Border(1).Padding(1).AlignCenter().Text(grandTotal > 0 ? (cat.Total / grandTotal * 100).ToString("N1") + "%" : "0%");
            }
        });
    }

    private void ComposeVoidsAndDiscounts(ColumnDescriptor col)
    {
        if (_summary.VoidEvents == null || !_summary.VoidEvents.Any()) return;
        
        ComposeSectionHeader(col, _isEnglish ? "Void Log" : "سجل الإلغاءات");
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(4); cd.RelativeColumn(3); });
            t.Header(h => {
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Order #" : "الرقم").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Staff" : "الموظف").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Amount" : "المبلغ").Bold();
            });

            foreach (var v in _summary.VoidEvents)
            {
                t.Cell().Border(1).Padding(1).AlignCenter().Text("#" + v.OrderId);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(v.VoidedByName);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(v.TotalVoidedAmount.ToString("N2")).Bold().FontColor(Colors.Black);
            }
        });
    }

    private void ComposeTopItems(ColumnDescriptor col)
    {
        ComposeSectionHeader(col, _isEnglish ? "Top Selling Items" : "الأصناف الأكثر مبيعاً");
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.RelativeColumn(5); cd.RelativeColumn(2); cd.RelativeColumn(3); });
            t.Header(h => {
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Item" : "الصنف").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Qty" : "الكمية").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Total" : "الإجمالي").Bold();
            });

            foreach (var itm in _catItems.OrderByDescending(x => x.TotalAmount).Take(10))
            {
                t.Cell().Border(1).Padding(1).AlignCenter().Text(itm.ItemName).Bold();
                t.Cell().Border(1).Padding(1).AlignCenter().Text(itm.Quantity.ToString("0.##"));
                t.Cell().Border(1).Padding(1).AlignCenter().Text(itm.TotalAmount.ToString("N2")).Bold();
            }
        });
    }

    private void ComposeStaffOrders(ColumnDescriptor col)
    {
        if (_summary.DetailedOrders == null) return;
        var staffOrders = _summary.DetailedOrders.Where(o => string.Equals(o.OrderType, "Staff", StringComparison.OrdinalIgnoreCase)).ToList();
        if (!staffOrders.Any()) return;

        ComposeSectionHeader(col, _isEnglish ? "Staff Meals Log" : "سجل وجبات العاملين");
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(3); cd.RelativeColumn(3.5f); cd.RelativeColumn(2); });
            t.Header(h => {
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "#" : "الرقم").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Responsible" : "المسئول").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Staff Name" : "اسم الموظف").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Time" : "الوقت").Bold();
            });

            foreach (var o in staffOrders)
            {
                t.Cell().Border(1).Padding(1).AlignCenter().Text("#" + o.OrderId);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(!string.IsNullOrEmpty(o.CashierName) ? o.CashierName : "-");
                t.Cell().Border(1).Padding(1).AlignCenter().Text(!string.IsNullOrEmpty(o.StaffMealEmployeeName) ? o.StaffMealEmployeeName : (o.TakeAwayCustomerName ?? "Worker"));
                t.Cell().Border(1).Padding(1).AlignCenter().Text(o.OrderDate?.ToString("HH:mm"));
            }
        });
    }

    private void ComposeHospitalityOrders(ColumnDescriptor col)
    {
        if (_summary.DetailedOrders == null) return;
        var hospitalityOrders = _summary.DetailedOrders.Where(o => string.Equals(o.OrderType, "Hospitality", StringComparison.OrdinalIgnoreCase)).ToList();
        if (!hospitalityOrders.Any()) return;

        ComposeSectionHeader(col, _isEnglish ? "Hospitality Orders Log" : "سجل طلبات الضيافة");
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(3); cd.RelativeColumn(3.5f); cd.RelativeColumn(2); });
            t.Header(h => {
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "#" : "الرقم").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Responsible" : "المسئول").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Reason" : "السبب").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(_isEnglish ? "Time" : "الوقت").Bold();
            });

            foreach (var o in hospitalityOrders)
            {
                t.Cell().Border(1).Padding(1).AlignCenter().Text("#" + o.OrderId);
                t.Cell().Border(1).Padding(1).AlignCenter().Text(!string.IsNullOrEmpty(o.HospitalityResponsibleName) ? o.HospitalityResponsibleName : (o.CashierName ?? "-"));
                t.Cell().Border(1).Padding(1).AlignCenter().Text(!string.IsNullOrEmpty(o.HospitalityReason) ? o.HospitalityReason : (o.TakeAwayCustomerName ?? "-"));
                t.Cell().Border(1).Padding(1).AlignCenter().Text(o.OrderDate?.ToString("HH:mm"));
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
