using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Catalog.Application.Contracts.Imports;
using WarehouseERP.Catalog.Application.Features.Imports;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.UnitTests.BuildingBlocks;

// The shared file writers and reader without a host: cell types, CSV safety and encoding, hostile uploads.
public sealed class DocumentFileTests
{
    private static readonly DocumentContext Arabic = new(
        DocumentLanguage.Ar, new DocumentIssuer("شركة", "Company", "123-456-789", "Africa/Cairo"),
        new DateTimeOffset(2026, 10, 30, 19, 0, 0, TimeSpan.FromHours(2)), "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");

    private static TabularReport Report(params object?[][] rows) => new(
        DocumentLanguage.Ar, "test-report", "تقرير",
        [new("كود", ColumnKind.Code), new("الاسم"), new("عدد", ColumnKind.Count), new("معامل", ColumnKind.Quantity)],
        rows, [new ReportFilter("الحالة", "نشط")]);

    [Theory]
    [InlineData("=HYPERLINK(\"x\")", "'=HYPERLINK(\"x\")")]
    [InlineData("+1", "'+1")]
    [InlineData("-ABC", "'-ABC")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("\tx", "'\tx")]
    [InlineData("زيت", "زيت")]
    [InlineData("", "")]
    public void Csv_text_that_a_spreadsheet_would_run_is_neutralised(string text, string expected) =>
        Assert.Equal(expected, CsvReportWriter.Neutralize(text));

    [Fact]
    public void Csv_is_utf8_with_bom_quoted_and_leaves_numbers_alone()
    {
        var csv = CsvReportWriter.Write(Report(["00123", "زيت، 5 لتر \"ممتاز\"", 3, -0.5m]), Arabic);

        Assert.Equal(Encoding.UTF8.GetPreamble(), csv[..3]);
        var lines = Encoding.UTF8.GetString(csv[3..]).Split("\r\n");
        Assert.Equal("كود,الاسم,عدد,معامل", lines[0]);
        Assert.Equal("00123,\"زيت، 5 لتر \"\"ممتاز\"\"\",3,-0.5", lines[1]);
    }

    [Fact]
    public void Xlsx_keeps_codes_as_text_numbers_as_numbers_and_reads_right_to_left_in_arabic()
    {
        var bytes = XlsxReportWriter.Write(Report(["00123", "=1+2", 7, 0.125m]), Arabic);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);
        Assert.True(sheet.RightToLeft);
        Assert.Equal(("00123", XLDataType.Text), (sheet.Cell(2, 1).GetString(), sheet.Cell(2, 1).DataType));
        Assert.False(sheet.Cell(2, 2).HasFormula);
        Assert.Equal((XLDataType.Number, 7d, 0.125d), (sheet.Cell(2, 3).DataType, sheet.Cell(2, 3).GetDouble(), sheet.Cell(2, 4).GetDouble()));
        Assert.Contains(workbook.Worksheet(2).RowsUsed(), r => r.Cell(1).GetString() == "الحالة" && r.Cell(2).GetString() == "نشط");
    }

    [Fact]
    public void Display_values_keep_exact_quantities_two_decimal_money_and_latin_digits()
    {
        Assert.Equal("1,234.5", ReportValues.Display(1234.5m, ColumnKind.Quantity, DocumentLanguage.Ar));
        Assert.Equal("0.12345678", ReportValues.Display(0.12345678m, ColumnKind.Quantity, DocumentLanguage.Ar));
        Assert.Equal("1,234.50", ReportValues.Display(1234.5m, ColumnKind.Money, DocumentLanguage.En));
        Assert.Equal("1234.5", ReportValues.Raw(1234.5m, ColumnKind.Quantity, DocumentLanguage.Ar));
        Assert.Equal("نعم", ReportValues.Display(true, ColumnKind.Text, DocumentLanguage.Ar));
    }

    [Fact]
    public void A_csv_upload_is_read_with_its_delimiter_bom_and_row_numbers()
    {
        var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("SKU *;Arabic name\r\n A-1 ;زيت\r\n;\r\nB-2;\"أرز; فاخر\"\r\n")).ToArray();

        var sheet = Assert.Single(TabularFileReader.ReadCsv(new MemoryStream(content), new ImportLimits()).Value);

        Assert.Equal(["SKU", "Arabic name"], sheet.Headers);
        Assert.Equal([2, 4], sheet.Rows.Select(r => r.Number));
        Assert.Equal(["A-1", "زيت"], sheet.Rows[0].Cells);
        Assert.Equal("أرز; فاخر", sheet.Rows[1].Cells[1]);
    }

    [Fact]
    public void Hostile_or_broken_workbooks_are_refused_as_unreadable()
    {
        var bomb = new MemoryStream();
        using (var zip = new ZipArchive(bomb, ZipArchiveMode.Create, leaveOpen: true))
        using (var entry = zip.CreateEntry("xl/workbook.xml", CompressionLevel.SmallestSize).Open())
            entry.Write(new byte[20 * 1024 * 1024]);

        foreach (var file in new[] { bomb.ToArray(), Encoding.UTF8.GetBytes("not a zip") })
        {
            var read = TabularFileReader.ReadXlsx(new MemoryStream(file), new ImportLimits());
            Assert.Equal("Import.File.Unreadable", Assert.Single(read.Error!.ValidationErrors).Code);
        }
    }

    [Fact]
    public void A_number_typed_into_excel_reads_back_as_its_digits()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Barcodes");
        sheet.Cell(1, 1).SetValue("Barcode");
        sheet.Cell(2, 1).Value = 6221234567891d;
        sheet.Cell(3, 1).Value = 0.5d;
        var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var rows = Assert.Single(TabularFileReader.ReadXlsx(new MemoryStream(stream.ToArray()), new ImportLimits()).Value).Rows;

        Assert.Equal(["6221234567891", "0.5"], rows.Select(r => r.Cells[0]));
    }

    [Theory]
    [InlineData("12", 12d)]
    [InlineData("0.125", 0.125)]
    [InlineData("1.50000000", 1.5)]
    [InlineData("0", null)]
    [InlineData("-3", null)]
    [InlineData("1,5", null)]
    [InlineData("0.123456789", null)]
    [InlineData("10000000000", null)]
    public void Conversion_factors_are_positive_dot_decimals_within_decimal_18_8(string text, double? expected)
    {
        var issues = new List<CatalogImportIssue>();
        var factor = new ImportRowCheck(issues, CatalogImportSheet.ProductUnits, 2).Factor("conversionFactor", text);

        Assert.Equal(expected is { } e ? (decimal?)(decimal)e : null, factor);
        Assert.Equal(expected is null, issues.Count == 1);
    }

    [Fact]
    public void Enum_cells_take_names_in_any_case_and_never_numbers()
    {
        var issues = new List<CatalogImportIssue>();
        var check = new ImportRowCheck(issues, CatalogImportSheet.Products, 2);

        Assert.Equal(ProductType.Service, check.Enum<ProductType>("type", "service", required: true));
        Assert.Null(check.Enum<ProductType>("type", "3", required: true));
        Assert.Null(check.Enum<TrackingMode>("trackingMode", null, required: false));
        Assert.Equal("Catalog.Import.InvalidValue", Assert.Single(issues).Code);
    }
}
