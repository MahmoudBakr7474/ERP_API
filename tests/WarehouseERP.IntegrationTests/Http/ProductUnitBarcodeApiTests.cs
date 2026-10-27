using System.Net;
using System.Text.Json;
using WarehouseERP.Catalog.Contracts;
using static WarehouseERP.IntegrationTests.Http.CatalogTesting;

namespace WarehouseERP.IntegrationTests.Http;

[Collection(DatabaseCollection.Name)]
public sealed class ProductUnitBarcodeApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;
    private readonly CollectingSink _logs = new();

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, logSink: _logs);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private static Task<HttpResponseMessage> LookupAsync(HttpClient client, string barcode) =>
        client.GetAsync($"{Products}/lookup?barcode={Uri.EscapeDataString(barcode)}");

    [Fact]
    public async Task A_product_gets_more_units_with_conversion_factors_and_keeps_its_base_unit_active()
    {
        var client = _api.CreateClient().As(AllPermissions);
        var piece = await CreateUnitAsync(client);
        var carton = await CreateUnitAsync(client);
        var kilo = await CreateUnitAsync(client, allowsFraction: true);
        var product = await CreateProductAsync(client, piece);

        var cartonUnit = await AddUnitAsync(client, product, carton, 12m);
        var details = await client.GetJsonAsync($"{Products}/{product}");
        var units = details.GetProperty("units").EnumerateArray().ToList();
        Assert.Equal(2, units.Count);
        var baseRow = units.Single(x => x.GetProperty("isBase").GetBoolean());
        var cartonRow = units.Single(x => x.GetProperty("id").GetGuid() == cartonUnit);
        Assert.Equal((piece, 1m), (baseRow.GetProperty("unitId").GetGuid(), baseRow.GetProperty("conversionFactor").GetDecimal()));
        Assert.Equal((12m, false, "قطعة"), (cartonRow.GetProperty("conversionFactor").GetDecimal(),
            cartonRow.GetProperty("isBase").GetBoolean(), cartonRow.GetProperty("unitNameAr").GetString()));
        Assert.Empty(cartonRow.GetProperty("barcodes").EnumerateArray());

        await (await client.PostJsonAsync($"{Products}/{product}/units", new { unitId = carton, conversionFactor = 24m }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Catalog.ProductUnit.UnitAlreadyAdded");
        await (await client.PostJsonAsync($"{Products}/{product}/units", new { unitId = kilo, conversionFactor = 0.5m }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.ProductUnit.FactorMustBeWhole");
        await (await client.PostJsonAsync($"{Products}/{product}/units", new { unitId = Guid.NewGuid(), conversionFactor = 2m }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Unit.NotActive");
        await (await client.PostJsonAsync($"{Products}/{Guid.NewGuid()}/units", new { unitId = carton, conversionFactor = 2m }))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Catalog.Product.NotFound");
        await (await client.PostJsonAsync($"{Products}/{product}/units", new { unitId = kilo, conversionFactor = 0m }))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");

        await (await client.PostEmptyAsync($"{Products}/{product}/units/{baseRow.GetProperty("id").GetGuid()}/deactivate"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.ProductUnit.BaseUnitMustStayActive");
        await (await client.PostEmptyAsync($"{Products}/{product}/units/{Guid.NewGuid()}/deactivate"))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Catalog.ProductUnit.NotFound");
        await client.PostEmptyAsync($"{Products}/{product}/units/{cartonUnit}/deactivate").AssertNoContentAsync();
        await client.PostEmptyAsync($"{Products}/{product}/units/{cartonUnit}/activate").AssertNoContentAsync();
    }

    [Fact]
    public async Task Barcodes_follow_their_type_and_a_value_is_never_reused()
    {
        var client = _api.CreateClient().As(AllPermissions);
        var piece = await CreateUnitAsync(client);
        var product = await CreateProductAsync(client, piece);
        var other = await CreateProductAsync(client, piece);
        var baseUnit = (await client.GetJsonAsync($"{Products}/{product}")).GetProperty("units")[0].GetProperty("id").GetGuid();
        var otherBase = (await client.GetJsonAsync($"{Products}/{other}")).GetProperty("units")[0].GetProperty("id").GetGuid();

        var ean = NewEan13();
        var wrongCheckDigit = ean[..12] + (char)('0' + (ean[12] - '0' + 1) % 10);
        var problem = await (await AddBarcodeAsync(client, product, baseUnit, wrongCheckDigit, BarcodeType.Ean13))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        Assert.Equal(("barcode", "BarcodeFormatValidator"),
            (problem.GetProperty("errors")[0].GetProperty("property").GetString(), problem.GetProperty("errors")[0].GetProperty("code").GetString()));

        var eanId = await AddBarcodeAsync(client, product, baseUnit, ean, BarcodeType.Ean13).CreatedIdAsync();
        // Internal codes are the company's own: no GS1 check digit.
        var internalCode = DatabaseFixture.Code("BC");
        await AddBarcodeAsync(client, product, baseUnit, internalCode).CreatedIdAsync();

        await (await AddBarcodeAsync(client, other, otherBase, ean, BarcodeType.Ean13))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Catalog.Barcode.Taken");
        // A retired barcode keeps its value.
        await client.PostEmptyAsync($"{Products}/{product}/barcodes/{eanId}/deactivate").AssertNoContentAsync();
        await (await AddBarcodeAsync(client, other, otherBase, ean, BarcodeType.Ean13))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Catalog.Barcode.Taken");

        var barcodes = (await client.GetJsonAsync($"{Products}/{product}")).GetProperty("units")[0].GetProperty("barcodes")
            .EnumerateArray().ToDictionary(x => x.GetProperty("barcode").GetString()!, x => x.GetProperty("isActive").GetBoolean());
        Assert.Equal(new Dictionary<string, bool> { [ean] = false, [internalCode] = true }, barcodes);

        await (await AddBarcodeAsync(client, product, Guid.NewGuid(), DatabaseFixture.Code("BC")))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Catalog.ProductUnit.NotFound");
        await (await client.PostEmptyAsync($"{Products}/{product}/barcodes/{Guid.NewGuid()}/activate"))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Catalog.Barcode.NotFound");
    }

    [Fact]
    public async Task Lookup_finds_only_active_barcodes_on_active_units_of_active_products()
    {
        var client = _api.CreateClient().As(AllPermissions);
        var piece = await CreateUnitAsync(client);
        var carton = await CreateUnitAsync(client);
        var product = await CreateProductAsync(client, piece);
        var cartonUnit = await AddUnitAsync(client, product, carton, 12m);
        var ean = NewEan13();
        var barcode = await AddBarcodeAsync(client, product, cartonUnit, ean, BarcodeType.Ean13).CreatedIdAsync();

        var item = await client.GetJsonAsync($"{Products}/lookup?barcode={ean}");
        Assert.Equal((product, cartonUnit, carton, 12m, false, barcode, "Ean13"),
            (item.GetProperty("productId").GetGuid(), item.GetProperty("productUnitId").GetGuid(), item.GetProperty("unitId").GetGuid(),
             item.GetProperty("conversionFactor").GetDecimal(), item.GetProperty("isBaseUnit").GetBoolean(),
             item.GetProperty("barcodeId").GetGuid(), item.GetProperty("barcodeType").GetString()));
        var search = await client.GetJsonAsync($"{Products}?search={ean}");
        Assert.Equal(product, Assert.Single(search.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());

        await client.PostEmptyAsync($"{Products}/{product}/barcodes/{barcode}/deactivate").AssertNoContentAsync();
        await (await LookupAsync(client, ean)).AssertProblemAsync(HttpStatusCode.NotFound, "Catalog.Barcode.NoSellableItem");
        await client.PostEmptyAsync($"{Products}/{product}/barcodes/{barcode}/activate").AssertNoContentAsync();
        Assert.Equal(HttpStatusCode.OK, (await LookupAsync(client, ean)).StatusCode);

        await client.PostEmptyAsync($"{Products}/{product}/units/{cartonUnit}/deactivate").AssertNoContentAsync();
        await (await LookupAsync(client, ean)).AssertProblemAsync(HttpStatusCode.NotFound, "Catalog.Barcode.NoSellableItem");
        // While its unit is retired the barcode can't come back, and the unit takes no new ones.
        await client.PostEmptyAsync($"{Products}/{product}/barcodes/{barcode}/deactivate").AssertNoContentAsync();
        await (await client.PostEmptyAsync($"{Products}/{product}/barcodes/{barcode}/activate"))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.ProductUnit.NotActive");
        await (await AddBarcodeAsync(client, product, cartonUnit, DatabaseFixture.Code("BC")))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.ProductUnit.NotActive");
        await client.PostEmptyAsync($"{Products}/{product}/units/{cartonUnit}/activate").AssertNoContentAsync();
        await client.PostEmptyAsync($"{Products}/{product}/barcodes/{barcode}/activate").AssertNoContentAsync();
        Assert.Equal(HttpStatusCode.OK, (await LookupAsync(client, ean)).StatusCode);

        await client.PostEmptyAsync($"{Products}/{product}/deactivate").AssertNoContentAsync();
        await (await LookupAsync(client, ean)).AssertProblemAsync(HttpStatusCode.NotFound, "Catalog.Barcode.NoSellableItem");
        await (await AddBarcodeAsync(client, product, cartonUnit, DatabaseFixture.Code("BC")))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Catalog.Product.NotActive");

        await (await LookupAsync(client, "")).AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
    }

    [Fact]
    public async Task Concurrent_barcode_claims_give_one_201_and_409s_never_500()
    {
        var client = _api.CreateClient().As(AllPermissions);
        var piece = await CreateUnitAsync(client);
        var targets = new List<(Guid Product, Guid Unit)>();
        for (var i = 0; i < 5; i++)
        {
            var product = await CreateProductAsync(client, piece);
            targets.Add((product, (await client.GetJsonAsync($"{Products}/{product}")).GetProperty("units")[0].GetProperty("id").GetGuid()));
        }
        var value = DatabaseFixture.Code("RACE");

        // Requests race past the pre-check; the unique index decides and its violation maps to the same code.
        var responses = await Task.WhenAll(targets.Select(t =>
            AddBarcodeAsync(_api.CreateClient().As(AllPermissions), t.Product, t.Unit, value)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        foreach (var conflict in responses.Where(r => r.StatusCode != HttpStatusCode.Created))
            await conflict.AssertProblemAsync(HttpStatusCode.Conflict, "Catalog.Barcode.Taken");
        Assert.DoesNotContain(_logs.Events, e => e.Level >= Serilog.Events.LogEventLevel.Error);
    }

    [Fact]
    public async Task Readers_can_scan_but_only_product_updaters_change_units_and_barcodes()
    {
        var admin = _api.CreateClient().As(AllPermissions);
        var piece = await CreateUnitAsync(admin);
        var carton = await CreateUnitAsync(admin);
        var product = await CreateProductAsync(admin, piece);
        var baseUnit = (await admin.GetJsonAsync($"{Products}/{product}")).GetProperty("units")[0].GetProperty("id").GetGuid();
        var code = DatabaseFixture.Code("BC");
        await AddBarcodeAsync(admin, product, baseUnit, code).CreatedIdAsync();

        var reader = _api.CreateClient().As(CatalogPermissions.ProductsRead);
        var item = await reader.GetJsonAsync($"{Products}/lookup?barcode={code}");
        Assert.True(item.GetProperty("isBaseUnit").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await AddBarcodeAsync(reader, product, baseUnit, DatabaseFixture.Code("BC"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await reader.PostJsonAsync($"{Products}/{product}/units", new { unitId = carton, conversionFactor = 6m })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostEmptyAsync($"{Products}/{product}/deactivate")).StatusCode);
    }
}
