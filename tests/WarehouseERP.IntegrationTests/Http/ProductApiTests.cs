using System.Net;
using System.Text.Json;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

[Collection(DatabaseCollection.Name)]
public sealed class ProductApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private const string Route = "/api/catalog/products";
    private const string AllPermissions =
        $"{CatalogPermissions.ProductsCreate},{CatalogPermissions.ProductsRead},{CatalogPermissions.ProductsUpdate}";
    private ApiFactory _api = null!;

    private readonly CollectingSink _logs = new();

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, logSink: _logs);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private static object NewProduct(Guid unitId, string? sku = null, ProductType type = ProductType.StockItem, TrackingMode tracking = TrackingMode.None) =>
        new { sku = sku ?? DatabaseFixture.Code("SKU"), nameAr = "صنف", nameEn = "Item", baseUnitId = unitId, categoryId = (Guid?)null, type, trackingMode = tracking };

    private static async Task<Guid> CreateAsync(HttpClient client, Guid unitId)
    {
        var response = await client.PostJsonAsync(Route, NewProduct(unitId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Create_then_read_returns_the_product_with_its_base_unit()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var client = _api.CreateClient().As(AllPermissions);
        var id = await CreateAsync(client, unitId);

        var product = await (await client.GetAsync($"{Route}/{id}")).ReadAsync<JsonElement>();

        Assert.Equal(("StockItem", "None"), (product.GetProperty("type").GetString(), product.GetProperty("trackingMode").GetString()));
        var unit = Assert.Single(product.GetProperty("units").EnumerateArray());
        Assert.Equal((unitId, 1m), (unit.GetProperty("unitId").GetGuid(), unit.GetProperty("conversionFactor").GetDecimal()));
    }

    [Fact]
    public async Task Tracking_a_service_is_422()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var response = await _api.CreateClient().As(AllPermissions)
            .PostJsonAsync(Route, NewProduct(unitId, type: ProductType.Service, tracking: TrackingMode.Serial));
        await response.AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Product.TrackingNeedsStockItem");
    }

    [Fact]
    public async Task Concurrent_creates_with_one_sku_give_one_201_and_409s_never_500()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var sku = DatabaseFixture.Code("SKU");
        var clients = Enumerable.Range(0, 6).Select(_ => _api.CreateClient().As(AllPermissions)).ToList();

        // Requests race past the SKU pre-check; the unique index decides and its violation maps to the same code.
        var responses = await Task.WhenAll(clients.Select(c => c.PostJsonAsync(Route, NewProduct(unitId, sku))));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        foreach (var conflict in responses.Where(r => r.StatusCode != HttpStatusCode.Created))
            await conflict.AssertProblemAsync(HttpStatusCode.Conflict, "Catalog.Product.SkuTaken");
        // A handled conflict is not an error in the logs.
        Assert.DoesNotContain(_logs.Events, e => e.Level >= Serilog.Events.LogEventLevel.Error);
    }

    [Fact]
    public async Task Update_with_the_current_row_version_succeeds_and_a_stale_one_is_409()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var client = _api.CreateClient().As(AllPermissions);
        var id = await CreateAsync(client, unitId);
        var original = (await (await client.GetAsync($"{Route}/{id}")).ReadAsync<JsonElement>()).GetProperty("rowVersion").GetString();

        var first = await client.PutJsonAsync($"{Route}/{id}", new { nameAr = "اسم جديد", nameEn = "New", rowVersion = original });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var next = (await first.ReadAsync<JsonElement>()).GetProperty("rowVersion").GetString();
        Assert.NotEqual(original, next);

        var stale = await client.PutJsonAsync($"{Route}/{id}", new { nameAr = "اسم تالت", nameEn = (string?)null, rowVersion = original });
        await stale.AssertProblemAsync(HttpStatusCode.Conflict, "Common.ConcurrencyConflict");

        var product = await (await client.GetAsync($"{Route}/{id}")).ReadAsync<JsonElement>();
        Assert.Equal("اسم جديد", product.GetProperty("nameAr").GetString());
    }

    [Fact]
    public async Task Update_with_a_malformed_row_version_is_400()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var client = _api.CreateClient().As(AllPermissions);
        var id = await CreateAsync(client, unitId);

        var response = await client.PutJsonAsync($"{Route}/{id}", new { nameAr = "اسم", rowVersion = "not-base64" });
        var problem = await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        Assert.Equal("RowVersionValidator", problem.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Search_matches_sku_or_names()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var client = _api.CreateClient().As(AllPermissions);
        var marker = DatabaseFixture.Code("FIND");
        await client.PostJsonAsync(Route, NewProduct(unitId, sku: marker));

        var page = await (await client.GetAsync($"{Route}?search={marker}")).ReadAsync<JsonElement>();
        Assert.Equal(marker, Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("sku").GetString());
    }
}
