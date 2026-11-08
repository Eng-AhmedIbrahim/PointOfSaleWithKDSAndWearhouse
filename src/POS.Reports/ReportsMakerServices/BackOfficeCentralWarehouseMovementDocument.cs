using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using POS.Core.Entities.Warehouse;
using System.Collections.Generic;
using System.Linq;
using System;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficeCentralWarehouseMovementDocument : IDocument
{
    private readonly List<StockLedger> _movements;
    private readonly string _branchName;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly string _reportTitle;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficeCentralWarehouseMovementDocument(
        List<StockLedger> movements,
        string branchName,
        DateTime fromDate,
        DateTime toDate,
        string reportTitle = "تقرير حركة وتوريدات الأصناف من المخزن الرئيسي",
        string lang = "ar",
        bool isThermal = false)
    {
        _movements = movements ?? new List<StockLedger>();
        _branchName = branchName;
        _fromDate = fromDate;
        _toDate = toDate;
        _reportTitle = reportTitle;
        _isThermal = isThermal;
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

            var baseStyle = TextStyle.Default.FontFamily("Arial", "Noto Sans Arabic").FontSize(_isThermal ? 8 : 9);
            page.DefaultTextStyle(baseStyle);

            if (!_isEnglish)
                page.ContentFromRightToLeft();

            page.Content().Column(col =>
            {
                col.Spacing(_isThermal ? 3 : 5);
                ComposeHeader(col);
                ComposeKpiSummary(col);
                ComposeMovementsTable(col);
                ComposeFooter(col);
            });
        });
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        col.Item().AlignCenter().Text(_branchName).FontSize(_isThermal ? 11 : 14).Bold().FontColor(Colors.Black);
        col.Item().AlignCenter().Text(_reportTitle).FontSize(_isThermal ? 12 : 16).Bold().FontColor(Colors.Teal.Darken3);
        col.Item().AlignCenter().Text(_isEnglish ? $"Period: {_fromDate:dd/MM/yyyy} - {_toDate:dd/MM/yyyy}" : $"الفترة من: {_fromDate:dd/MM/yyyy} إلى: {_toDate:dd/MM/yyyy}").FontSize(_isThermal ? 7 : 9).FontColor(Colors.Grey.Darken2);
        col.Item().PaddingVertical(3).LineHorizontal(1).LineColor(Colors.Black);
    }

    private void ComposeKpiSummary(ColumnDescriptor col)
    {
        int totalMovements = _movements.Count;
        decimal totalQtyIn = _movements.Sum(m => m.QuantityIn);
        decimal totalCost = _movements.Sum(m => m.TotalCost);
        int uniqueProducts = _movements.Select(m => m.ProductId).Distinct().Count();

        if (_isThermal)
        {
            col.Item().Background(Colors.Grey.Lighten4).Padding(3).Column(c =>
            {
                c.Item().Row(r =>
                {
                    r.RelativeItem().Text(_isEnglish ? "Movements Count:" : "عدد الحركات:").Bold().FontSize(8);
                    r.RelativeItem().AlignRight().Text(totalMovements.ToString()).Bold().FontSize(8);
                });
                c.Item().Row(r =>
                {
                    r.RelativeItem().Text(_isEnglish ? "Total Qty In:" : "إجمالي الكمية الواردة:").Bold().FontSize(8);
                    r.RelativeItem().AlignRight().Text(totalQtyIn.ToString("G29")).Bold().FontSize(8).FontColor(Colors.Teal.Darken3);
                });
                c.Item().Row(r =>
                {
                    r.RelativeItem().Text(_isEnglish ? "Total Value:" : "إجمالي القيمة:").Bold().FontSize(8);
                    r.RelativeItem().AlignRight().Text($"{totalCost:N2}").Bold().FontSize(8);
                });
            });
        }
        else
        {
            col.Item().PaddingVertical(3).Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Row(row =>
            {
                void Kpi(string label, string val, string color)
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().AlignCenter().Text(label).FontSize(8).FontColor(Colors.Grey.Darken2);
                        c.Item().AlignCenter().Text(val).FontSize(12).Bold().FontColor(color);
                    });
                }

                Kpi(_isEnglish ? "Total Movements" : "عدد حركات التوريد", totalMovements.ToString(), Colors.Teal.Darken4);
                Kpi(_isEnglish ? "Total Qty In" : "إجمالي الكمية المستلمة", totalQtyIn.ToString("G29"), Colors.Blue.Darken3);
                Kpi(_isEnglish ? "Total Value" : "إجمالي القيمة التقديرية", $"{totalCost:N2}", Colors.Green.Darken3);
                Kpi(_isEnglish ? "Unique Products" : "الأصناف المختلفة", uniqueProducts.ToString(), Colors.Purple.Darken3);
            });
        }
    }

    private void ComposeMovementsTable(ColumnDescriptor col)
    {
        if (!_movements.Any())
        {
            col.Item().Padding(10).AlignCenter().Text(_isEnglish ? "No stock movements from central warehouse found." : "لا توجد حركات توريد من المخزن الرئيسي في هذه الفترة.").FontSize(9).FontColor(Colors.Grey.Darken1);
            return;
        }

        col.Item().PaddingTop(4).Table(t =>
        {
            if (_isThermal)
            {
                t.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(4); // Item
                    cd.RelativeColumn(2.5f); // Qty
                    cd.RelativeColumn(2.5f); // Time
                });

                t.Header(h =>
                {
                    h.Cell().BorderBottom(1).Padding(1).AlignRight().Text(_isEnglish ? "Item" : "الصنف").Bold().FontSize(7);
                    h.Cell().BorderBottom(1).Padding(1).AlignCenter().Text(_isEnglish ? "Qty" : "الكمية").Bold().FontSize(7);
                    h.Cell().BorderBottom(1).Padding(1).AlignCenter().Text(_isEnglish ? "Time" : "الوقت").Bold().FontSize(7);
                });

                foreach (var m in _movements.OrderByDescending(x => x.MovementDate))
                {
                    string name = m.Product?.NameAr ?? m.Product?.NameEn ?? $"#{m.ProductId}";
                    t.Cell().BorderBottom(0.5f).Padding(1).AlignRight().Text(name).FontSize(7);
                    t.Cell().BorderBottom(0.5f).Padding(1).AlignCenter().Text(m.QuantityIn.ToString("G29")).Bold().FontSize(7);
                    t.Cell().BorderBottom(0.5f).Padding(1).AlignCenter().Text(m.MovementDate.ToString("MM/dd HH:mm")).FontSize(7);
                }
            }
            else
            {
                t.ColumnsDefinition(cd =>
                {
                    cd.ConstantColumn(100); // Date & Time
                    cd.ConstantColumn(80);  // SKU
                    cd.RelativeColumn(3);   // Product Name
                    cd.ConstantColumn(75);  // Qty In
                    cd.ConstantColumn(70);  // Unit Cost
                    cd.ConstantColumn(80);  // Total Cost
                    cd.ConstantColumn(75);  // Balance
                    cd.ConstantColumn(90);  // Ref Doc #
                    cd.RelativeColumn(2);   // Notes / User
                });

                t.Header(h =>
                {
                    void H(string txt) => h.Cell().Background(Colors.Teal.Darken3).Padding(3).AlignCenter().Text(txt).Bold().FontSize(8).FontColor(Colors.White);
                    H(_isEnglish ? "Date & Time" : "التاريخ والوقت");
                    H(_isEnglish ? "SKU / Code" : "كود الصنف");
                    H(_isEnglish ? "Product Name" : "اسم الصنف / الخامة");
                    H(_isEnglish ? "Qty In" : "الكمية الواردة");
                    H(_isEnglish ? "Unit Cost" : "سعر الوحدة");
                    H(_isEnglish ? "Total Cost" : "إجمالي القيمة");
                    H(_isEnglish ? "Balance" : "الرصيد بعد الحركة");
                    H(_isEnglish ? "Ref Doc #" : "رقم المستند / الطلب");
                    H(_isEnglish ? "Notes / By" : "الملاحظات والمسؤول");
                });

                int idx = 0;
                foreach (var m in _movements.OrderByDescending(x => x.MovementDate))
                {
                    var bg = idx % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                    idx++;

                    void C(string txt, bool bold = false, string? clr = null)
                    {
                        var cell = t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(3).AlignCenter().Text(txt ?? "-").FontSize(7.5f);
                        if (bold) cell.Bold();
                        if (!string.IsNullOrEmpty(clr)) cell.FontColor(clr);
                    }

                    string prodName = _isEnglish ? (m.Product?.NameEn ?? m.Product?.NameAr ?? $"Item #{m.ProductId}") : (m.Product?.NameAr ?? m.Product?.NameEn ?? $"صنف #{m.ProductId}");
                    string sku = m.Product?.SKU ?? "-";

                    C(m.MovementDate.ToString("yyyy-MM-dd HH:mm"));
                    C(sku);
                    t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(3).AlignRight().Text(prodName).Bold().FontSize(7.5f);
                    C(m.QuantityIn.ToString("G29"), bold: true, clr: Colors.Teal.Darken4);
                    C(m.UnitCost.ToString("N2"));
                    C(m.TotalCost.ToString("N2"), bold: true);
                    C(m.RunningBalance.ToString("G29"));
                    C(m.ReferenceDocumentType ?? "REQ");
                    C(m.Notes ?? "-");
                }
            }
        });
    }

    private void ComposeFooter(ColumnDescriptor col)
    {
        col.Item().PaddingTop(8).AlignCenter().Text(text =>
        {
            text.Span(_isEnglish ? "Printed at: " : "طبع في: ").FontSize(7);
            text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).FontSize(7);
        });
    }
}
