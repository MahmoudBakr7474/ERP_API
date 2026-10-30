using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using WarehouseERP.Catalog.Contracts;
using static WarehouseERP.IntegrationTests.Http.CatalogFiles;

namespace WarehouseERP.IntegrationTests.Http;

// Catalog downloads from the real host: the full filtered result (not the screen's page), typed cells, codes kept as
// text, Arabic and formula-safe CSV, PDFs with the right document information, limits and authorization.
[Collection(DatabaseCollection.Name)]
public sealed class CatalogExportApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Reader() => _api.CreateClient().As(CatalogTesting.AllPermissions);

    // Products whose Arabic name carries a token, so a search finds exactly them in the shared database.
    private async Task<(string Token, List<Guid> Ids, Guid UnitId)> ProductsAsync(int count, string? nameEn = "Item")
    {
        var client = Reader();
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var token = $"T{Guid.NewGuid():N}"[..12];
        var ids = new List<Guid>();
        for (var i = 0; i < count; i++)
            ids.Add(await client.PostJsonAsync(CatalogTesting.Products, new
            {
                sku = $"{token}-{i:000}", nameAr = $"صنف {token} رقم {i}", nameEn, baseUnitId = unitId, categoryId = (Guid?)null,
                type = ProductType.StockItem, trackingMode = TrackingMode.None,
            }).CreatedIdAsync());
        return (token, ids, unitId);
    }

    [Fact]
    public async Task An_export_holds_the_whole_filtered_result_not_the_screens_page()
    {
        var (token, _, _) = await ProductsAsync(23);
        var client = Reader();

        var page = await client.GetJsonAsync($"{CatalogTesting.Products}?search={token}");
        Assert.Equal((23, 20), (page.GetProperty("totalCount").GetInt32(), page.GetProperty("items").GetArrayLength()));

        var response = await client.GetAsync($"{Exports}/products?format=Xlsx&lang=En&search={token}&sort=Sku&descending=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Xlsx, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Matches(@"^catalog-products-\d{8}-\d{4}\.xlsx$", response.Content.Headers.ContentDisposition?.FileNameStar);
        Assert.True(response.Headers.CacheControl?.NoStore);

        using var workbook = Read(await response.Content.ReadAsByteArrayAsync());
        var sheet = workbook.Worksheets.First();
        Assert.Equal(24, sheet.LastRowUsed()!.RowNumber()); // header + 23
        Assert.Equal("SKU", sheet.Cell(1, 1).GetString());
        Assert.Equal($"{token}-022", sheet.Cell(2, 1).GetString()); // the requested order
        Assert.Equal(XLDataType.Text, sheet.Cell(2, 1).DataType);
        var activeUnits = Enumerable.Range(1, 13).Single(c => sheet.Cell(1, c).GetString() == "Active units");
        Assert.Equal((XLDataType.Number, 1d), (sheet.Cell(2, activeUnits).DataType, sheet.Cell(2, activeUnits).GetDouble()));
        Assert.True(sheet.SheetView.SplitRow == 1 && sheet.AutoFilter.IsEnabled);
        Assert.Equal("Report info", workbook.Worksheets.Last().Name);
    }

    [Fact]
    public async Task Codes_keep_their_leading_zeros_and_text_never_runs_as_a_formula()
    {
        var (token, ids, _) = await ProductsAsync(1, nameEn: "=HYPERLINK(\"http://evil\",\"x\")");
        var client = Reader();
        var product = await client.GetJsonAsync($"{CatalogTesting.Products}/{ids[0]}");
        var gtin = WithCheckDigit("00" + Random.Shared.NextInt64(1_000_000_000, 9_999_999_999));
        Assert.Equal(HttpStatusCode.Created,
            (await CatalogTesting.AddBarcodeAsync(client, ids[0], product.GetProperty("units")[0].GetProperty("id").GetGuid(), gtin, BarcodeType.Gtin)).StatusCode);

        using (var workbook = Read(await client.DownloadAsync($"{Exports}/barcodes?format=Xlsx&lang=En&search={token}", Xlsx)))
        {
            var cell = workbook.Worksheets.First().Cell(2, 1);
            Assert.Equal((gtin, XLDataType.Text), (cell.GetString(), cell.DataType));
        }

        using (var workbook = Read(await client.DownloadAsync($"{Exports}/products?format=Xlsx&lang=En&search={token}", Xlsx)))
        {
            var englishName = workbook.Worksheets.First().Cell(2, 3);
            Assert.False(englishName.HasFormula);
            Assert.StartsWith("=HYPERLINK", englishName.GetString());
        }

        var csv = await client.DownloadAsync($"{Exports}/products?format=Csv&lang=Ar&search={token}", "text/csv");
        Assert.Equal(Encoding.UTF8.GetPreamble(), csv[..3]);
        var text = Encoding.UTF8.GetString(csv[3..]);
        Assert.StartsWith("كود الصنف,الاسم العربي,الاسم الإنجليزي", text);
        Assert.Contains($"صنف {token} رقم 0", text);
        Assert.Contains("\"'=HYPERLINK(\"\"http://evil\"\",\"\"x\"\")\"", text); // quoted, escaped and neutralised

        var barcodes = Encoding.UTF8.GetString(await client.DownloadAsync($"{Exports}/barcodes?format=Csv&search={token}", "text/csv"));
        Assert.Contains($"\r\n{gtin},", barcodes);
    }

    [Fact]
    public async Task A_pdf_is_a_branded_multi_page_document_with_its_title_and_company()
    {
        var (token, ids, _) = await ProductsAsync(45);
        var client = Reader();

        var pdf = await client.DownloadAsync($"{Exports}/products?format=Pdf&lang=Ar&search={token}", "application/pdf");

        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
        var raw = Encoding.Latin1.GetString(pdf);
        Assert.True(Regex.Count(raw, @"/Type\s*/Page\b") >= 2, "45 products need more than one page.");
        AssertPdfHas(pdf, "قائمة الأصناف الرئيسية"); // the document title
        AssertPdfHas(pdf, "الشركة"); // the issuing company (the test company Arabic name), as author

        var sheet = await client.DownloadAsync($"{CatalogTesting.Products}/{ids[0]}/sheet?lang=En", "application/pdf");
        AssertPdfHas(sheet, "Product specification sheet");
        await (await client.GetAsync($"{CatalogTesting.Products}/{Guid.NewGuid()}/sheet")).AssertProblemAsync(HttpStatusCode.NotFound, "Catalog.Product.NotFound");
    }

    [Fact]
    public async Task Too_many_rows_for_a_format_is_a_clear_refusal()
    {
        var (token, _, _) = await ProductsAsync(3);
        await using var api = new ApiFactory(fx.ConnectionString, settings: new Dictionary<string, string?> { ["Documents:MaxPdfRows"] = "2" });
        var client = api.CreateClient().As(CatalogTesting.AllPermissions);

        await (await client.GetAsync($"{Exports}/products?format=Pdf&search={token}"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Export.TooManyRows");
        await client.DownloadAsync($"{Exports}/products?format=Xlsx&search={token}", Xlsx);
    }

    [Fact]
    public async Task Downloading_needs_the_catalog_read_permission_and_a_known_format()
    {
        foreach (var url in new[] { "products", "product-units", "barcodes", "categories", "units", "data-quality" })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await _api.CreateClient().As("inventory.stock.read").GetAsync($"{Exports}/{url}")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _api.CreateClient().GetAsync($"{Exports}/{url}")).StatusCode);
            await Reader().DownloadAsync($"{Exports}/{url}?format=Csv&isActive=false&search=nothing-matches-this", "text/csv");
        }
        var invalid = await (await Reader().GetAsync($"{Exports}/units?format=7")).AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        Assert.Equal("format", invalid.GetProperty("errors")[0].GetProperty("property").GetString());
    }

    [Fact]
    public async Task Categories_units_and_unit_conversions_export_with_their_counts()
    {
        var client = Reader();
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var box = await CatalogTesting.CreateUnitAsync(client);
        var categoryId = await CatalogTesting.CreateCategoryAsync(client);
        var productId = await CatalogTesting.CreateProductAsync(client, unitId, categoryId);
        await CatalogTesting.AddUnitAsync(client, productId, box, 12);
        var category = await client.GetJsonAsync($"{CatalogTesting.Categories}/{categoryId}");
        var sku = (await client.GetJsonAsync($"{CatalogTesting.Products}/{productId}")).GetProperty("sku").GetString();

        using (var workbook = Read(await client.DownloadAsync($"{Exports}/categories?format=Xlsx&lang=En", Xlsx)))
        {
            var sheet = workbook.Worksheets.First();
            var row = sheet.RowsUsed().Single(r => r.Cell(1).GetString() == category.GetProperty("code").GetString());
            Assert.Equal((1d, 0d, 1d), (row.Cell(7).GetDouble(), row.Cell(8).GetDouble(), row.Cell(9).GetDouble()));
        }

        using (var workbook = Read(await client.DownloadAsync($"{Exports}/product-units?format=Xlsx&lang=En&search={sku}", Xlsx)))
        {
            var sheet = workbook.Worksheets.First();
            Assert.Equal(3, sheet.LastRowUsed()!.RowNumber()); // header, the base unit (1), the box (12)
            Assert.Equal((1d, 12d), (sheet.Cell(2, 7).GetDouble(), sheet.Cell(3, 7).GetDouble()));
        }

        var pdf = await client.DownloadAsync($"{Exports}/categories?format=Pdf&lang=En", "application/pdf");
        AssertPdfHas(pdf, "Category list and product counts");
    }

    // Document metadata strings are written as UTF-16BE hex when they hold non-ASCII text, as plain text otherwise.
    private static void AssertPdfHas(byte[] pdf, string text)
    {
        var expected = text.All(char.IsAscii) ? text : "FEFF" + Convert.ToHexString(Encoding.BigEndianUnicode.GetBytes(text));
        Assert.Contains(expected, Encoding.Latin1.GetString(pdf), StringComparison.OrdinalIgnoreCase);
    }
}
