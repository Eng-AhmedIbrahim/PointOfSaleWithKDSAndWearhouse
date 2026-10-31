using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System;

namespace POS.Reports.ReportsMakerServices;

public class BackOfficeRecipeReportDocument : IDocument
{
    private readonly IEnumerable<dynamic> _recipes;
    private readonly string _branchName;
    private readonly bool _isEnglish;
    private readonly bool _isThermal;

    public BackOfficeRecipeReportDocument(IEnumerable<dynamic> recipes, string branchName, string lang = "ar", bool isThermal = false)
    {
        _recipes = recipes ?? new List<dynamic>();
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
                ComposeRecipes(col);
                ComposeFooter(col);
            });
        });
    }

    private void ComposeHeader(ColumnDescriptor col)
    {
        col.Item().AlignCenter().Text(_branchName).FontSize(_isThermal ? 11 : 14).Bold().FontColor(Colors.Black);
        col.Item().AlignCenter().Text(_isEnglish ? "Products Recipes Report" : "تقرير مكونات الأصناف (الرسبي)").FontSize(_isThermal ? 12 : 18).Bold().FontColor(Colors.Black);
        
        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
    }

    private void ComposeRecipes(ColumnDescriptor col)
    {
        var groups = _recipes.GroupBy(r => (string)r.ProductName).ToList();

        foreach (var g in groups)
        {
            col.Item().PaddingTop(10).Column(rCol =>
            {
                // Product Title (Table Header)
                rCol.Item().Table(t => {
                    t.ColumnsDefinition(cd => cd.RelativeColumn());
                    t.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text(g.Key).Bold();
                });

                // Ingredients Table
                rCol.Item().Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(5); // Ingredient
                        cd.RelativeColumn(2); // Quantity
                        cd.RelativeColumn(2); // Unit
                    });

                    t.Header(h =>
                    {
                        h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Ingredient" : "المكون").Bold().FontSize(8);
                        h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Qty" : "الكمية").Bold().FontSize(8);
                        h.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(1).AlignCenter().Text(_isEnglish ? "Unit" : "وحدة").Bold().FontSize(8);
                    });

                    foreach (var ing in g)
                    {
                        t.Cell().Border(1).Padding(1).AlignRight().Text((string)ing.IngredientName).FontSize(8);
                        t.Cell().Border(1).Padding(1).AlignCenter().Text(((decimal)ing.Quantity).ToString("0.####")).FontSize(8);
                        t.Cell().Border(1).Padding(1).AlignCenter().Text((string)ing.Unit).FontSize(8);
                    }
                });
            });
        }
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
