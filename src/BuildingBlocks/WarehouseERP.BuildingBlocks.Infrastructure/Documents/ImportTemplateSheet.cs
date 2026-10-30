using ClosedXML.Excel;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>
/// The pieces of an import template workbook: a data sheet (headers in the chosen language, required ones navy and
/// marked *, a help note on each header, code and text columns stored as text so leading zeros survive, drop-downs for
/// choices, frozen header) and the styled header cells of reference lists. The module writes its instructions and lists.
/// </summary>
public static class ImportTemplateSheet
{
    public const int Rows = 5_000;

    /// <param name="lists">Choice column key → the range holding its labels (the drop-down's source).</param>
    public static IXLWorksheet Add(
        XLWorkbook workbook, string name, IReadOnlyList<ImportColumn> columns, bool arabic, IReadOnlyDictionary<string, IXLRange> lists)
    {
        var sheet = workbook.Worksheets.Add(name);
        sheet.RightToLeft = arabic;
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var header = sheet.Cell(1, i + 1);
            header.Value = column.Header(arabic);
            header.Style.Font.Bold = true;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml(column.Required ? BrandedPdf.Navy : "#1E4E8C");
            header.CreateComment().AddText(arabic ? column.HelpAr : column.HelpEn);
            var cells = sheet.Range(2, i + 1, Rows + 1, i + 1);
            if (column.Kind is ImportColumnKind.Code or ImportColumnKind.Text)
                cells.Style.NumberFormat.Format = "@";
            if (column.Choices is { } choices && lists.TryGetValue(column.Key, out var range))
            {
                var validation = cells.CreateDataValidation();
                validation.List(range, true);
                validation.IgnoreBlanks = true;
                validation.ErrorStyle = XLErrorStyle.Stop;
                validation.ErrorTitle = column.Header(arabic);
                validation.ErrorMessage = string.Join(" / ", choices.Select(x => arabic ? x.LabelAr : x.LabelEn));
            }
            sheet.Column(i + 1).Width = Math.Max(16, column.Header(arabic).Length + 6);
        }
        sheet.SheetView.FreezeRows(1);
        return sheet;
    }

    /// <summary>A reference list's header cell: navy, white, bold.</summary>
    public static void Head(IXLCell cell, string text)
    {
        cell.SetValue(text);
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = XLColor.White;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(BrandedPdf.Navy);
    }
}
