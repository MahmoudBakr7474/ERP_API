using System.Net;
using System.Text.Json;
using WarehouseERP.Catalog.Contracts;
using static WarehouseERP.IntegrationTests.Http.CatalogFiles;

namespace WarehouseERP.IntegrationTests.Http;

// The catalog report screens' data: counts and issues use the same definitions as the downloads.
[Collection(DatabaseCollection.Name)]
public sealed class CatalogReportApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Reader() => _api.CreateClient().As(CatalogTesting.AllPermissions);

    [Fact]
    public async Task The_data_quality_report_finds_missing_barcodes_categories_and_inconsistent_units()
    {
        var client = Reader();
        var unitCode = DatabaseFixture.Code("KG");
        var kg = await client.PostJsonAsync(CatalogTesting.Units, new { code = unitCode, nameAr = "كجم", nameEn = "kg", allowsFraction = true })
            .CreatedIdAsync();
        var half = await CatalogTesting.CreateUnitAsync(client, allowsFraction: true);
        var productId = await CatalogTesting.CreateProductAsync(client, kg);
        await CatalogTesting.AddUnitAsync(client, productId, half, 0.5m);
        var sku = (await client.GetJsonAsync($"{CatalogTesting.Products}/{productId}")).GetProperty("sku").GetString();

        // The base unit stops allowing fractions after the half unit was added: the 0.5 factor is now inconsistent.
        var unit = await client.GetJsonAsync($"{CatalogTesting.Units}/{kg}");
        Assert.Equal(HttpStatusCode.OK, (await client.PutJsonAsync($"{CatalogTesting.Units}/{kg}",
            new { nameAr = "كجم", nameEn = "kg", allowsFraction = false, rowVersion = unit.GetProperty("rowVersion").GetString() })).StatusCode);

        var report = await client.GetJsonAsync($"{Reports}/data-quality?search={sku}&isActive=true");
        var issues = report.GetProperty("issues").GetProperty("items").EnumerateArray()
            .Select(i => (i.GetProperty("issue").GetString(), i.GetProperty("detail").ValueKind == JsonValueKind.Null ? null : i.GetProperty("detail").GetString()))
            .ToList();

        Assert.Contains(("NoBarcode", null), issues);
        Assert.Contains(("NoCategory", null), issues);
        var fractional = Assert.Single(issues, i => i.Item1 == "FractionalFactorOnWholeUnit");
        var detail = fractional.Item2!;
        Assert.Equal($"= 0.5 {unitCode}", detail[detail.IndexOf('=')..]);
        var counts = report.GetProperty("counts").EnumerateArray().ToDictionary(c => c.GetProperty("issue").GetString()!, c => c.GetProperty("count").GetInt32());
        Assert.Equal(1, counts["FractionalFactorOnWholeUnit"]);

        var filtered = await client.GetJsonAsync($"{Reports}/data-quality?search={sku}&issue=NoBarcode");
        Assert.Equal(1, filtered.GetProperty("issues").GetProperty("totalCount").GetInt32());

        // The summary counts the same products.
        var summary = await client.GetJsonAsync($"{Reports}/summary");
        Assert.True(summary.GetProperty("productsWithUnitIssues").GetInt32() >= 1);
        Assert.True(summary.GetProperty("productsWithoutBarcode").GetInt32() >= 1);
    }

    [Fact]
    public async Task Category_counts_and_unit_conversions_follow_the_catalog()
    {
        var client = Reader();
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var parent = await CatalogTesting.CreateCategoryAsync(client);
        var child = await CatalogTesting.CreateCategoryAsync(client, parent);
        var active = await CatalogTesting.CreateProductAsync(client, unitId, child);
        var retired = await CatalogTesting.CreateProductAsync(client, unitId, child);
        await client.PostEmptyAsync($"{CatalogTesting.Products}/{retired}/deactivate").AssertNoContentAsync();

        var rows = (await client.GetJsonAsync($"{Reports}/categories")).EnumerateArray().ToList();
        var parentAt = rows.FindIndex(r => r.GetProperty("id").ValueKind != JsonValueKind.Null && r.GetProperty("id").GetGuid() == parent);
        var childRow = rows[parentAt + 1];
        Assert.Equal((child, 1, 1, 1), (childRow.GetProperty("id").GetGuid(), childRow.GetProperty("level").GetInt32(),
            childRow.GetProperty("activeProducts").GetInt32(), childRow.GetProperty("inactiveProducts").GetInt32()));

        var box = await CatalogTesting.CreateUnitAsync(client);
        await CatalogTesting.AddUnitAsync(client, active, box, 24);
        var conversions = await client.GetJsonAsync($"{Reports}/unit-conversions?categoryId={child}&isActive=true");
        var items = conversions.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal((24m, false), (items[1].GetProperty("conversionFactor").GetDecimal(), items[1].GetProperty("isBase").GetBoolean()));
    }

    [Fact]
    public async Task Reports_need_the_catalog_read_permission()
    {
        foreach (var url in new[] { "summary", "categories", "data-quality", "unit-conversions" })
            Assert.Equal(HttpStatusCode.Forbidden, (await _api.CreateClient().As("inventory.stock.read").GetAsync($"{Reports}/{url}")).StatusCode);
        await (await Reader().GetAsync($"{Reports}/data-quality?pageSize=1000"))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
    }

    [Fact]
    public async Task The_product_list_sorts_on_request()
    {
        var client = Reader();
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var token = $"S{Guid.NewGuid():N}"[..10];
        foreach (var (sku, name) in new[] { ("B", "ج"), ("A", "ب"), ("C", "أ") })
            await client.PostJsonAsync(CatalogTesting.Products, new
            {
                sku = $"{token}-{sku}", nameAr = $"{name} {token}", nameEn = (string?)null, baseUnitId = unitId, categoryId = (Guid?)null,
                type = ProductType.StockItem, trackingMode = TrackingMode.None,
            }).CreatedIdAsync();

        async Task<string[]> Skus(string query) => (await client.GetJsonAsync($"{CatalogTesting.Products}?search={token}&{query}"))
            .GetProperty("items").EnumerateArray().Select(i => i.GetProperty("sku").GetString()![^1..]).ToArray();

        Assert.Equal(["A", "B", "C"], await Skus(""));
        Assert.Equal(["C", "B", "A"], await Skus("sort=Sku&descending=true"));
        Assert.Equal(["C", "A", "B"], await Skus("sort=NameAr"));
    }
}
