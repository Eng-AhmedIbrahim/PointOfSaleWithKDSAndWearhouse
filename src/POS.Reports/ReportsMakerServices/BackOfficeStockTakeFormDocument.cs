using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficeStockTakeFormDocument : IDocument
{
    private readonly IEnumerable<dynamic> _inventory;
    private readonly string _branchName;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficeStockTakeFormDocument(IEnumerable<dynamic> inventory, string branchName, string lang = "ar", bool isThermal = false)
    {
        _inventory = inventory ?? new List<dynamic>();
        _branchName = branchName;
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
                col.Spacing(_isThermal ? 3 : 6);
                ComposeHeader(col);
                ComposeFormTable(col);
                ComposeFooter(col);
            });
        });
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        col.Item().AlignCenter().Text(_branchName).FontSize(_isThermal ? 11 : 14).Bold().FontColor(Colors.Black);
        col.Item().AlignCenter().Text(_isEnglish ? "Physical Stock Take Form" : "نموذج الجرد الفعلي للمخازن").FontSize(_isThermal ? 12 : 18).Bold().FontColor(Colors.Black);
        
        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
        col.Item().PaddingBottom(5).AlignCenter().Text(_isEnglish ? "Please write the actual quantities manually in the 'Actual Count' column" : "يرجى كتابة الكميات الفعلية يدوياً في خانة 'الجرد الفعلي'").FontSize(_isThermal ? 6 : 9).Italic();
    }

    private void ComposeFormTable(ColumnDescriptor col)
    {
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(1.2f); // ID
                cd.RelativeColumn(4);    // Item Name
                cd.RelativeColumn(2);    // Category
                cd.RelativeColumn(1.8f); // Unit
                cd.RelativeColumn(2);    // System Balance
                cd.RelativeColumn(2.5f); // Actual Count (Writing cell)
            });

            t.Header(h =>
            {
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "ID" : "الكود").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Item" : "اسم الصنف").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Category" : "الفئة").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Unit" : "وحدة").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "System" : "رصيد السيستم").Bold();
                h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Actual" : "الجرد الفعلي").Bold();
            });

            foreach (var inv in _inventory)
            {
                t.Cell().Border(1).Padding(1).AlignCenter().Text((string)(inv.Id?.ToString() ?? "-")).FontSize(_isThermal ? 7 : 9);
                t.Cell().Border(1).Padding(1).AlignRight().Text((string)(inv.ItemName?.ToString() ?? "-")).FontSize(_isThermal ? 7 : 9).Bold();
                t.Cell().Border(1).Padding(1).AlignCenter().Text((string)(inv.Category?.ToString() ?? "-")).FontSize(_isThermal ? 7 : 8);
                t.Cell().Border(1).Padding(1).AlignCenter().Text((string)(inv.Unit?.ToString() ?? "-")).FontSize(_isThermal ? 7 : 9);
                t.Cell().Border(1).Padding(1).AlignCenter().Text((string)(inv.SystemQty?.ToString("0.###") ?? "0")).FontSize(_isThermal ? 7 : 9);
                t.Cell().Border(1).PaddingTop(_isThermal ? 10 : 15).BorderColor(Colors.Black); // Large empty cell for manual writing
            }
        });
    }

    private void ComposeFooter(ColumnDescriptor col)
    {
        col.Item().PaddingTop(25).AlignCenter().Row(r => {
            r.RelativeItem().PaddingHorizontal(20).Column(c => {
                c.Item().LineHorizontal(1).LineColor(Colors.Black);
                c.Item().AlignCenter().Text(_isEnglish ? "Store Keeper Signature" : "توقيع أمين المخزن").FontSize(8).Bold();
                c.Item().PaddingTop(15).PaddingHorizontal(5).Text("........................................").FontSize(7);
            });
            r.RelativeItem().PaddingHorizontal(20).Column(c => {
                c.Item().LineHorizontal(1).LineColor(Colors.Black);
                c.Item().AlignCenter().Text(_isEnglish ? "Branch Manager Signature" : "توقيع مدير الفرع").FontSize(8).Bold();
                c.Item().PaddingTop(15).PaddingHorizontal(5).Text("........................................").FontSize(7);
            });
        });
        
        col.Item().PaddingTop(15).AlignCenter().Text(text =>
        {
            text.Span(_isEnglish ? "Printed at: " : "طبع في: ").FontSize(7);
            text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).FontSize(7);
        });
    }
}
