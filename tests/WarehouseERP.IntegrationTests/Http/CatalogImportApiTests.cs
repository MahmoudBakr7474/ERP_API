using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog.Contracts;
using static WarehouseERP.IntegrationTests.Http.CatalogFiles;

namespace WarehouseERP.IntegrationTests.Http;

// The catalog import against SQL Server and the real host: preview changes nothing, commit is all or nothing and only
// what was previewed, modes decide what happens to existing records, and every rule of the single-record endpoints
// holds for imported rows.
[Collection(DatabaseCollection.Name)]
public sealed class CatalogImportApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(CatalogTesting.AllPermissions);

    private static string Id(string prefix) => DatabaseFixture.Code(prefix);

    [Fact]
    public async Task A_full_workbook_creates_units_categories_products_units_and_barcodes_in_dependency_order()
    {
        var client = Admin();
        var (pcs, box, food, oil, sku) = (Id("PCS"), Id("BOX"), Id("FOOD"), Id("OIL"), Id("SKU"));
        var barcode = WithCheckDigit("00" + Random.Shared.NextInt64(1_000_000_000, 9_999_999_999));
        var file = Workbook(
            ("Units", [UnitsHeader, Row(pcs, "قطعة", "Piece", "No"), Row(box, "كرتونة", "Box", "لا")]),
            ("Categories", [CategoriesHeader, Row(oil, "زيوت", "Oils", food), Row(food, "مواد غذائية", null, null)]),
            ("Products", [ProductsHeader, Row(sku, "زيت عباد الشمس 5 لتر", "Sunflower oil 5L", pcs, oil, "صنف مخزني", null)]),
            ("ProductUnits", [ProductUnitsHeader, Row(sku, box, 4), Row(sku, pcs, 1)]),
            ("Barcodes", [BarcodesHeader, Row(sku, pcs, barcode, "Gtin"), Row(sku, box, $"BOX-{sku}", "داخلي")]));

        var preview = await PreviewOkAsync(client, file, "Create");
        Assert.True(preview.GetProperty("canCommit").GetBoolean(), preview.ToString());
        Assert.Equal((9, 8, 0, 1), (Total(preview, "rows"), Total(preview, "create"), Total(preview, "invalid"), Total(preview, "unchanged")));
        // The preview changed nothing.
        Assert.Equal(0, (await client.GetJsonAsync($"{CatalogTesting.Products}?search={sku}")).GetProperty("totalCount").GetInt32());

        var result = await CommitAsync(client, file, "Create", preview.GetProperty("previewToken").GetString()!);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        var lookup = await client.GetJsonAsync($"{CatalogTesting.Products}/lookup?barcode=BOX-{sku}");
        Assert.Equal((sku, 4m, box), (lookup.GetProperty("sku").GetString(), lookup.GetProperty("conversionFactor").GetDecimal(),
            lookup.GetProperty("unitCode").GetString()));
        var gtin = await client.GetJsonAsync($"{CatalogTesting.Products}/lookup?barcode={barcode}");
        Assert.True(gtin.GetProperty("isBaseUnit").GetBoolean());
        var product = await client.GetJsonAsync($"{CatalogTesting.Products}/{lookup.GetProperty("productId").GetGuid()}");
        Assert.Equal("StockItem", product.GetProperty("type").GetString());
        var categories = await client.GetJsonAsync(CatalogTesting.Categories);
        var oilCategory = categories.EnumerateArray().Single(c => c.GetProperty("code").GetString() == oil);
        var foodCategory = categories.EnumerateArray().Single(c => c.GetProperty("code").GetString() == food);
        Assert.Equal(foodCategory.GetProperty("id").GetGuid(), oilCategory.GetProperty("parentId").GetGuid());
        Assert.Equal(oilCategory.GetProperty("id").GetGuid(), product.GetProperty("categoryId").GetGuid());
    }

    [Fact]
    public async Task A_sheet_without_a_required_column_is_reported_and_nothing_can_be_imported()
    {
        var file = Workbook(("Products", [Row("Arabic name", "Base unit code", "Product type"), Row("صنف", "PCS", "StockItem")]));

        var preview = await PreviewOkAsync(Admin(), file, "Create");

        Assert.False(preview.GetProperty("canCommit").GetBoolean());
        Assert.Contains(("Products", 1, "sku", "Catalog.Import.MissingColumn"), Issues(preview));
    }

    [Fact]
    public async Task Invalid_values_are_reported_with_their_row_and_column_all_at_once()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var unit = (await Admin().GetJsonAsync($"{CatalogTesting.Units}/{unitId}")).GetProperty("code").GetString();
        var file = Workbook(("Products",
        [
            ProductsHeader,
            Row("BAD SKU!", null, null, unit, null, "Gadget", null),
            Row(Id("SKU"), "خدمة", null, unit, null, "Service", "Serial"),
            Row(Id("SKU"), new string('x', 251), null, "NO-SUCH-UNIT", "NO-SUCH-CAT", "StockItem", "None"),
        ]));

        var issues = Issues(await PreviewOkAsync(Admin(), file, "Create"));

        Assert.Contains(("Products", 2, "sku", "Catalog.Import.InvalidFormat"), issues);
        Assert.Contains(("Products", 2, "nameAr", "Catalog.Import.Required"), issues);
        Assert.Contains(("Products", 2, "type", "Catalog.Import.InvalidValue"), issues);
        Assert.Contains(("Products", 3, "trackingMode", "Catalog.Product.TrackingNeedsStockItem"), issues);
        Assert.Contains(("Products", 4, "nameAr", "Catalog.Import.TooLong"), issues);
        Assert.Contains(("Products", 4, "baseUnitCode", "Catalog.Unit.NotFound"), issues);
        Assert.Contains(("Products", 4, "categoryCode", "Catalog.Category.NotFound"), issues);
    }

    [Fact]
    public async Task Duplicates_in_the_file_and_barcodes_already_used_are_errors()
    {
        var client = Admin();
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var unit = (await client.GetJsonAsync($"{CatalogTesting.Units}/{unitId}")).GetProperty("code").GetString()!;
        var existingId = await CatalogTesting.CreateProductAsync(client, unitId);
        var existing = await client.GetJsonAsync($"{CatalogTesting.Products}/{existingId}");
        var used = $"USED-{Id("B")}";
        await CatalogTesting.AddBarcodeAsync(client, existingId, existing.GetProperty("units")[0].GetProperty("id").GetGuid(), used);
        var (sku, twice) = (Id("SKU"), Id("SKU"));
        var file = Workbook(
            ("Products", [ProductsHeader, Row(twice, "أ", null, unit, null, "StockItem", null), Row(twice, "ب", null, unit, null, "StockItem", null),
                Row(sku, "ج", null, unit, null, "StockItem", null)]),
            ("Barcodes", [BarcodesHeader, Row(sku, unit, used, "Internal"), Row(sku, unit, $"X-{sku}", "Internal"), Row(sku, unit, $"x-{sku}", "Other")]));

        var preview = await PreviewOkAsync(client, file, "Create");
        var issues = Issues(preview);

        Assert.Contains(("Products", 2, "sku", "Catalog.Import.DuplicateInFile"), issues);
        Assert.Contains(("Products", 3, "sku", "Catalog.Import.DuplicateInFile"), issues);
        Assert.Contains(("Barcodes", 2, "barcode", "Catalog.Barcode.Taken"), issues);
        // Barcodes are unique regardless of case, as in the database.
        Assert.Contains(("Barcodes", 3, "barcode", "Catalog.Import.DuplicateInFile"), issues);
        Assert.Contains(("Barcodes", 4, "barcode", "Catalog.Import.DuplicateInFile"), issues);
        Assert.True(Total(preview, "duplicates") >= 4);
        Assert.False(preview.GetProperty("canCommit").GetBoolean());
    }

    [Fact]
    public async Task Conversion_factors_follow_the_unit_rules()
    {
        var client = Admin();
        var (_, piece) = await fx.SeedBranchAndUnitAsync(); // whole pieces only
        var pieceCode = (await client.GetJsonAsync($"{CatalogTesting.Units}/{piece}")).GetProperty("code").GetString()!;
        var (half, sku) = (Id("HALF"), Id("SKU"));
        var file = Workbook(
            ("Units", [UnitsHeader, Row(half, "نصف", null, "No")]),
            ("Products", [ProductsHeader, Row(sku, "صنف", null, pieceCode, null, "StockItem", null)]),
            ("ProductUnits", [ProductUnitsHeader, Row(sku, half, 0.5), Row(sku, pieceCode, 2), Row(sku, Id("U"), "abc"), Row(sku, Id("U"), -3)]));

        var issues = Issues(await PreviewOkAsync(client, file, "Create"));

        Assert.Contains(("ProductUnits", 2, "conversionFactor", "Catalog.ProductUnit.FactorMustBeWhole"), issues);
        Assert.Contains(("ProductUnits", 3, "conversionFactor", "Catalog.Import.BaseUnitFactor"), issues);
        Assert.Contains(("ProductUnits", 4, "conversionFactor", "Catalog.Import.InvalidNumber"), issues);
        Assert.Contains(("ProductUnits", 5, "conversionFactor", "Catalog.Import.InvalidNumber"), issues);
    }

    [Fact]
    public async Task Create_refuses_existing_codes_skip_leaves_them_and_update_changes_only_editable_fields()
    {
        var client = Admin();
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var unit = (await client.GetJsonAsync($"{CatalogTesting.Units}/{unitId}")).GetProperty("code").GetString()!;
        var existingId = await CatalogTesting.CreateProductAsync(client, unitId);
        var existingSku = (await client.GetJsonAsync($"{CatalogTesting.Products}/{existingId}")).GetProperty("sku").GetString()!;
        var file = Workbook(("Products",
            [ProductsHeader, Row(existingSku, "اسم جديد", "New name", unit, null, "StockItem", null), Row(Id("SKU"), "صنف جديد", null, unit, null, "StockItem", null)]));

        var create = await PreviewOkAsync(client, file, "Create");
        Assert.Contains(("Products", 2, "sku", "Catalog.Product.SkuTaken"), Issues(create));
        Assert.False(create.GetProperty("canCommit").GetBoolean());

        var skip = await PreviewOkAsync(client, file, "Skip");
        Assert.Equal(("Skip", "Create"), (Action(skip, "Products", 2), Action(skip, "Products", 3)));

        var update = await PreviewOkAsync(client, file, "Update");
        Assert.Equal(("Update", "Create"), (Action(update, "Products", 2), Action(update, "Products", 3)));
        Assert.Equal(HttpStatusCode.OK,
            (await CommitAsync(client, file, "Update", update.GetProperty("previewToken").GetString()!)).StatusCode);
        var updated = await client.GetJsonAsync($"{CatalogTesting.Products}/{existingId}");
        Assert.Equal(("اسم جديد", "New name", existingSku), (updated.GetProperty("nameAr").GetString(),
            updated.GetProperty("nameEn").GetString(), updated.GetProperty("sku").GetString()));

        // Fixed fields can't be "updated" by an import, and nothing is half-applied.
        var otherUnit = (await fx.SeedBranchAndUnitAsync()).UnitId;
        var otherCode = (await client.GetJsonAsync($"{CatalogTesting.Units}/{otherUnit}")).GetProperty("code").GetString()!;
        var fixedFields = Workbook(("Products", [ProductsHeader, Row(existingSku, "اسم", null, otherCode, null, "Service", null)]));
        var issues = Issues(await PreviewOkAsync(client, fixedFields, "Update"));
        Assert.Contains(("Products", 2, "baseUnitCode", "Catalog.Import.FixedField"), issues);
        Assert.Contains(("Products", 2, "type", "Catalog.Import.FixedField"), issues);

        // Update finds nothing to change the second time.
        var again = await PreviewOkAsync(client, Workbook(("Products", [ProductsHeader, Row(existingSku, "اسم جديد", null, unit, null, "StockItem", null)])), "Update");
        Assert.Equal("Unchanged", Action(again, "Products", 2));
    }

    [Fact]
    public async Task A_change_after_the_preview_stops_the_commit_and_nothing_is_imported()
    {
        var client = Admin();
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var (newUnit, first, second) = (Id("U"), Id("SKU"), Id("SKU"));
        var file = Workbook(
            ("Units", [UnitsHeader, Row(newUnit, "وحدة", null, "No")]),
            ("Products", [ProductsHeader, Row(first, "أول", null, newUnit, null, "StockItem", null), Row(second, "ثان", null, newUnit, null, "StockItem", null)]));
        var preview = await PreviewOkAsync(client, file, "Create");
        Assert.True(preview.GetProperty("canCommit").GetBoolean());

        // Someone creates one of the file's products meanwhile.
        await client.PostJsonAsync(CatalogTesting.Products, new
        {
            sku = second, nameAr = "سبقني", nameEn = (string?)null, baseUnitId = unitId, categoryId = (Guid?)null,
            type = ProductType.StockItem, trackingMode = TrackingMode.None,
        }).CreatedIdAsync();

        await (await CommitAsync(client, file, "Create", preview.GetProperty("previewToken").GetString()!))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Catalog.Import.ChangedSincePreview");
        Assert.Equal(0, (await client.GetJsonAsync($"{CatalogTesting.Products}?search={first}")).GetProperty("totalCount").GetInt32());
        Assert.Equal(0, (await client.GetJsonAsync($"{CatalogTesting.Units}?search={newUnit}")).GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Confirming_the_same_preview_twice_imports_once()
    {
        var client = Admin();
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var unit = (await client.GetJsonAsync($"{CatalogTesting.Units}/{unitId}")).GetProperty("code").GetString()!;
        var sku = Id("SKU");
        var file = Workbook(("Products", [ProductsHeader, Row(sku, "مرة واحدة", null, unit, null, "StockItem", null)]));
        var token = (await PreviewOkAsync(client, file, "Create")).GetProperty("previewToken").GetString()!;

        Assert.Equal(HttpStatusCode.OK, (await CommitAsync(client, file, "Create", token)).StatusCode);
        await (await CommitAsync(client, file, "Create", token)).AssertProblemAsync(HttpStatusCode.Conflict, "Catalog.Import.ChangedSincePreview");

        Assert.Equal(1, (await client.GetJsonAsync($"{CatalogTesting.Products}?search={sku}")).GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task A_failure_while_saving_rolls_the_whole_import_back()
    {
        var client = Admin();
        var (newUnit, category, sku) = (Id("U"), Id("C"), Id("SKU"));
        var poisoned = $"FAIL-{Guid.NewGuid():N}";
        var trigger = $"TR_Test_{Guid.NewGuid():N}";
        var file = Workbook(
            ("Units", [UnitsHeader, Row(newUnit, "وحدة", null, "No")]),
            ("Categories", [CategoriesHeader, Row(category, "تصنيف", null, null)]),
            ("Products", [ProductsHeader, Row(sku, "صنف", null, newUnit, category, "StockItem", null)]),
            ("Barcodes", [BarcodesHeader, Row(sku, newUnit, poisoned, "Internal")]));
        var token = (await PreviewOkAsync(client, file, "Create")).GetProperty("previewToken").GetString()!;

        // The barcode is the last row the import writes; the database refuses it, as any late failure would.
        var create = $"CREATE TRIGGER catalog.[{trigger}] ON catalog.ProductBarcodes AFTER INSERT AS " +
                     $"IF EXISTS (SELECT 1 FROM inserted WHERE Barcode = '{poisoned}') THROW 50001, 'Refused by the test.', 1;";
        var drop = $"DROP TRIGGER catalog.[{trigger}];";
        await using (var db = fx.NewDb())
            await db.Database.ExecuteSqlRawAsync(create);
        try
        {
            var response = await CommitAsync(client, file, "Create", token);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
        finally
        {
            await using var db = fx.NewDb();
            await db.Database.ExecuteSqlRawAsync(drop);
        }

        Assert.Equal(0, (await client.GetJsonAsync($"{CatalogTesting.Units}?search={newUnit}")).GetProperty("totalCount").GetInt32());
        Assert.Equal(0, (await client.GetJsonAsync($"{CatalogTesting.Products}?search={sku}")).GetProperty("totalCount").GetInt32());
        Assert.DoesNotContain((await client.GetJsonAsync(CatalogTesting.Categories)).EnumerateArray(),
            c => c.GetProperty("code").GetString() == category);
    }

    [Fact]
    public async Task Each_row_needs_the_permission_of_what_it_changes()
    {
        var unit = Id("U");
        var file = Workbook(("Units", [UnitsHeader, Row(unit, "وحدة", null, "No")]),
            ("Products", [ProductsHeader, Row(Id("SKU"), "صنف", null, unit, null, "StockItem", null)]));

        // Read only: nothing to import with.
        await (await PreviewAsync(_api.CreateClient().As(CatalogPermissions.ProductsRead), file, "Create"))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Catalog.Import.NotPermitted");
        // No catalog permission at all: the endpoint itself refuses.
        Assert.Equal(HttpStatusCode.Forbidden, (await PreviewAsync(_api.CreateClient().As("inventory.stock.read"), file, "Create")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PreviewAsync(_api.CreateClient(), file, "Create")).StatusCode);

        // May create products but not units: the unit row is refused, so the product's reference to it is too.
        var preview = await PreviewOkAsync(
            _api.CreateClient().As($"{CatalogPermissions.ProductsRead},{CatalogPermissions.ProductsCreate}"), file, "Create");
        var issues = Issues(preview);
        Assert.Contains(("Units", 2, null, "Catalog.Import.NotPermitted"), issues);
        Assert.Contains(("Products", 2, "baseUnitCode", "Catalog.Import.ReferenceInvalid"), issues);
        Assert.False(preview.GetProperty("canCommit").GetBoolean());
    }

    [Fact]
    public async Task Unreadable_unsupported_hostile_and_oversized_files_are_refused_clearly()
    {
        var client = Admin();
        async Task Refused(byte[] content, string name, string code, string? sheet = null)
        {
            var problem = await (await PreviewAsync(client, content, "Create", name, sheet)).AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
            Assert.Equal(code, problem.GetProperty("errors")[0].GetProperty("code").GetString());
        }

        await Refused(Encoding.UTF8.GetBytes("not a workbook"), "catalog.xlsx", "Import.File.Unreadable");
        await Refused(Encoding.UTF8.GetBytes("a,b"), "catalog.txt", "Import.File.UnsupportedType");
        await Refused(new byte[5 * 1024 * 1024 + 1], "big.csv", "Import.File.TooLarge", "Products");
        await Refused(ZipBomb(), "bomb.xlsx", "Import.File.Unreadable");
        await Refused(Encoding.UTF8.GetBytes("SKU\nA"), "products.csv", "Catalog.Import.SheetRequired");
        var tooMany = Workbook(("Products", [ProductsHeader, .. Enumerable.Range(0, 5_001).Select(i => Row($"S{i}", "x", null, "U", null, "StockItem", null))]));
        await Refused(tooMany, "many.xlsx", "Import.File.TooManyRows");

        var noMode = new MultipartFormDataContent { { new ByteArrayContent(Workbook(("Units", [UnitsHeader]))), "file", "a.xlsx" } };
        var missingMode = await (await client.PostAsync($"{Imports}/preview", noMode)).AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        Assert.Equal("mode", missingMode.GetProperty("errors")[0].GetProperty("property").GetString());
    }

    [Fact]
    public async Task A_csv_file_imports_one_kind_of_rows()
    {
        var client = Admin();
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var unit = (await client.GetJsonAsync($"{CatalogTesting.Units}/{unitId}")).GetProperty("code").GetString()!;
        var sku = Id("SKU");
        // Excel in many locales saves CSV with semicolons; the reader detects the delimiter.
        var csv = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes($"كود الصنف;الاسم العربي;كود الوحدة الأساسية;نوع الصنف\r\n{sku};\"زيت، 1 لتر\";{unit};صنف مخزني\r\n")).ToArray();

        var preview = await PreviewOkAsync(client, csv, "Create", "products.csv", "Products");
        Assert.True(preview.GetProperty("canCommit").GetBoolean(), preview.ToString());
        Assert.Equal(HttpStatusCode.OK,
            (await CommitAsync(client, csv, "Create", preview.GetProperty("previewToken").GetString()!, "products.csv", "Products")).StatusCode);

        var product = (await client.GetJsonAsync($"{CatalogTesting.Products}?search={sku}")).GetProperty("items")[0];
        Assert.Equal("زيت، 1 لتر", product.GetProperty("nameAr").GetString());
    }

    [Fact]
    public async Task The_template_and_the_error_report_are_workbooks_in_the_chosen_language()
    {
        var client = Admin();
        using (var template = Read(await client.DownloadAsync($"{Imports}/template?lang=En", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")))
        {
            Assert.Equal(["Instructions", "Lists", "Units", "Categories", "Products", "ProductUnits", "Barcodes"],
                template.Worksheets.Select(w => w.Name));
            Assert.Equal("SKU *", template.Worksheet("Products").Cell(1, 1).GetString());
            Assert.Equal(0, template.Worksheet("Products").RangeUsed()!.RowCount() - 1);
        }
        using (var arabic = Read(await client.DownloadAsync($"{Imports}/template", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")))
            Assert.True(arabic.Worksheet("الأصناف").RightToLeft);

        var file = Workbook(("Products", [ProductsHeader, Row("BAD SKU", null, null, "U", null, "StockItem", null)]));
        var form = new MultipartFormDataContent { { new ByteArrayContent(file), "file", "bad.xlsx" }, { new StringContent("Create"), "mode" } };
        var response = await client.PostAsync($"{Imports}/error-report?lang=En", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var report = Read(await response.Content.ReadAsByteArrayAsync());
        var sheet = report.Worksheets.First();
        Assert.Equal(["Sheet", "Row", "Column", "Value", "Error", "Code"], Enumerable.Range(1, 6).Select(c => sheet.Cell(1, c).GetString()));
        Assert.Contains(Enumerable.Range(2, sheet.LastRowUsed()!.RowNumber() - 1),
            r => sheet.Cell(r, 3).GetString() == "SKU" && sheet.Cell(r, 6).GetString() == "Catalog.Import.InvalidFormat");
    }

    // A tiny .xlsx whose one entry would inflate to 60 MB of zeros.
    private static byte[] ZipBomb()
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entry = zip.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.SmallestSize).Open();
            var zeros = new byte[1024 * 1024];
            for (var i = 0; i < 60; i++)
                entry.Write(zeros);
        }
        return stream.ToArray();
    }
}
