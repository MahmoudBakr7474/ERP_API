using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>
/// A <see cref="TabularReport"/> as CSV: UTF-8 with a byte-order mark (so Excel opens Arabic correctly), comma
/// separated, CRLF lines, RFC 4180 quoting, one header row in the report's language, invariant numbers ("1234.5")
/// and ISO dates. Text that a spreadsheet would run as a formula is neutralised.
/// </summary>
public static class CsvReportWriter
{
    public static byte[] Write(TabularReport report, DocumentContext context)
    {
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { NewLine = "\r\n" }))
        {
            foreach (var column in report.Columns)
                csv.WriteField(Neutralize(column.Header));
            csv.NextRecord();
            foreach (var row in report.Rows)
            {
                for (var c = 0; c < report.Columns.Count; c++)
                {
                    var value = row[c];
                    var text = ReportValues.Raw(value, report.Columns[c].Kind, context.Language);
                    csv.WriteField(ReportValues.IsNumber(value) ? text : Neutralize(text));
                }
                csv.NextRecord();
            }
        }
        return stream.ToArray();
    }

    /// <summary>
    /// OWASP CSV injection: a cell starting with = + - @ tab or carriage return is prefixed with an apostrophe, so
    /// Excel and LibreOffice show it as text instead of evaluating it. Numbers written as numbers are left alone.
    /// </summary>
    public static string Neutralize(string text) =>
        text.Length > 0 && text[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + text : text;
}
