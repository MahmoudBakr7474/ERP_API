using System.Globalization;
using ClosedXML.Excel;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>
/// A <see cref="TabularReport"/> as an Excel workbook built for analysis: the data sheet starts at A1 with one header
/// row (frozen, filterable), numbers are numeric cells, codes are text cells (leading zeros kept), dates are dates.
/// A second sheet states the title, company, filters and generation time. Arabic workbooks read right to left.
/// </summary>
public static class XlsxReportWriter
{
    private const int MaxColumnWidth = 60;

    public static byte[] Write(TabularReport report, DocumentContext context)
    {
        using var workbook = new XLWorkbook();
        workbook.Properties.Title = report.Title;
        workbook.Properties.Author = context.CompanyName ?? "WarehouseERP";
        workbook.Properties.Company = context.CompanyName ?? "";

        var data = workbook.Worksheets.Add(SheetName(report.Title));
        data.RightToLeft = context.IsArabic;
        WriteTable(data, report, context);

        var info = workbook.Worksheets.Add(context.L("معلومات التقرير", "Report info"));
        info.RightToLeft = context.IsArabic;
        WriteInfo(info, report, context);

        data.SetTabActive();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteTable(IXLWorksheet sheet, TabularReport report, DocumentContext context)
    {
        var columns = report.Columns;
        var widths = columns.Select(c => c.Header.Length + 2).ToArray();
        for (var c = 0; c < columns.Count; c++)
        {
            var cell = sheet.Cell(1, c + 1);
            cell.Value = columns[c].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(BrandedPdf.Navy);
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }
        sheet.Row(1).Height = 22;

        var row = 2;
        foreach (var values in report.Rows)
        {
            for (var c = 0; c < columns.Count; c++)
                widths[c] = Math.Max(widths[c], SetCell(sheet.Cell(row, c + 1), values[c], columns[c], context));
            row++;
        }
        var lastDataRow = row - 1;

        if (report.Totals is { } totals)
        {
            for (var c = 0; c < columns.Count; c++)
            {
                var cell = sheet.Cell(row, c + 1);
                SetCell(cell, totals[c], columns[c], context);
                cell.Style.Font.Bold = true;
                cell.Style.Border.TopBorder = XLBorderStyleValues.Medium;
                cell.Style.Border.TopBorderColor = XLColor.FromHtml(BrandedPdf.Navy);
            }
        }

        // The filter covers the header and data rows only, never the totals.
        sheet.Range(1, 1, Math.Max(lastDataRow, 1), columns.Count).SetAutoFilter();
        sheet.SheetView.FreezeRows(1);
        for (var c = 0; c < columns.Count; c++)
            sheet.Column(c + 1).Width = Math.Min(MaxColumnWidth, widths[c] + 1);
    }

    // Returns the text length the column needs for this value.
    private static int SetCell(IXLCell cell, object? value, ReportColumn column, DocumentContext context)
    {
        switch (value)
        {
            case null:
                return 0;
            case DateTime date:
                cell.Value = date;
                cell.Style.NumberFormat.Format = "yyyy-mm-dd hh:mm";
                return 16;
            case DateTimeOffset date:
                cell.Value = date.DateTime;
                cell.Style.NumberFormat.Format = "yyyy-mm-dd hh:mm";
                return 16;
            case var _ when ReportValues.IsNumber(value) && column.Kind is not (ColumnKind.Code or ColumnKind.Text):
                var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                cell.Value = number;
                cell.Style.NumberFormat.Format = column.Kind switch
                {
                    ColumnKind.Money => "#,##0.00",
                    ColumnKind.Count => "#,##0",
                    _ => "General", // a conversion factor keeps every decimal it has, none added
                };
                return ReportValues.Display(number, column.Kind, context.Language).Length;
            default:
                var text = ReportValues.Display(value, column.Kind, context.Language);
                // Text, never a formula: SetValue of a string stores a string even when it starts with "=".
                cell.SetValue(text);
                if (column.Kind == ColumnKind.Code)
                    cell.Style.NumberFormat.Format = "@";
                return text.Length;
        }
    }

    private static void WriteInfo(IXLWorksheet sheet, TabularReport report, DocumentContext context)
    {
        var lines = new List<(string Label, string Value)>
        {
            (context.L("التقرير", "Report"), report.Title),
            (context.L("الشركة", "Company"), context.CompanyName ?? ""),
        };
        if (context.Issuer?.TaxRegistrationNumber is { } taxNumber)
            lines.Add((context.L("الرقم الضريبي", "Tax registration no."), taxNumber));
        lines.Add((context.L("تاريخ الإنشاء", "Generated"),
            context.GeneratedAt.ToString(ReportValues.DateTimeFormat, CultureInfo.InvariantCulture)));
        lines.Add((context.L("عدد السجلات", "Rows"), report.Rows.Count.ToString(CultureInfo.InvariantCulture)));
        lines.AddRange((report.Summary ?? []).Select(s => (s.Label, s.Value)));
        if (report.Filters.Count > 0)
        {
            lines.Add(("", ""));
            lines.Add((context.L("عوامل التصفية", "Filters"), ""));
            lines.AddRange(report.Filters.Select(f => (f.Label, f.Value)));
        }

        for (var i = 0; i < lines.Count; i++)
        {
            sheet.Cell(i + 1, 1).SetValue(lines[i].Label);
            sheet.Cell(i + 1, 1).Style.Font.Bold = true;
            sheet.Cell(i + 1, 2).SetValue(lines[i].Value);
        }
        sheet.Cell(1, 2).Style.Font.Bold = true;
        sheet.Cell(1, 2).Style.Font.FontColor = XLColor.FromHtml(BrandedPdf.Navy);
        sheet.Column(1).Width = 24;
        sheet.Column(2).Width = 60;
    }

    // Excel sheet names: at most 31 characters, none of : \ / ? * [ ].
    private static string SheetName(string title)
    {
        var name = new string(title.Where(ch => ch is not (':' or '\\' or '/' or '?' or '*' or '[' or ']')).ToArray()).Trim();
        return name.Length == 0 ? "Report" : name.Length > 31 ? name[..31] : name;
    }
}
